using System;
using System.IO;
using ScoreManagerForSchool.Core.Security;
using ScoreManagerForSchool.Core.Storage;

namespace ScoreManagerForSchool.Cli
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var baseDir = Path.Combine(Directory.GetCurrentDirectory(), "base");
			var store = new Database1Store(baseDir);
			var model = store.Load();
			if (model == null)
			{
				// 继续下面的首次运行流程
			}

			if (model == null || string.IsNullOrEmpty(model.ID1))
			{
				Console.WriteLine("检测到首次运行，进入 OOBE 设置。");
				Console.WriteLine("设置用户密码（用于数据库解密，长度8~256）");
				var (userPwd, userHint) = ReadPasswordConfirmed("用户密码");

				// 生成设备id
				var deviceId = DeviceIdGenerator.GenerateDeviceId();
				Console.WriteLine($"生成设备ID: {deviceId.Substring(0,16)}... (省略)");

				var randomHex = DeviceIdGenerator.GenerateDeviceId().ToUpper();
				var payload = "0D0007211145141919810" + randomHex;

				// 使用 PBKDF2 派生 key，并持久化盐与迭代次数
				var rawSalt1 = Guid.NewGuid().ToByteArray();
				var salt1 = Convert.ToBase64String(rawSalt1);
				var key1 = CryptoUtil.DeriveKey(userPwd, rawSalt1, 32, 100000);
				var id1 = CryptoUtil.EncryptToBase64(payload, key1);


				model = new Database1Model { ID1 = id1, Salt1 = salt1, Iterations = 100000 };
				store.Save(model);

				// 保存密码提示（仅用户密码）
				var hintStore = new PwhintStore(baseDir);
				string?[] hintsToSave = new string?[] { userHint };
				hintStore.SaveHints(hintsToSave);

				Console.WriteLine("OOBE 完成并保存 base/Database1.json");

				// 询问是否导入 CSV
				Console.WriteLine("是否现在导入学生名单 CSV？(y/n)");
				if (Console.ReadLine()?.Trim().ToLower() == "y")
				{
					Console.WriteLine("请输入学生名单 CSV 路径: ");
					var p = Console.ReadLine();
					if (!string.IsNullOrEmpty(p) && File.Exists(p))
					{
						var students = CsvImporter.ImportStudents(p);
						var sstore = new StudentStore(baseDir);
						sstore.Save(students);
						Console.WriteLine($"已导入 {students.Count} 个学生并保存到 base/students.json");
					}
				}

				Console.WriteLine("是否现在导入班级列表 CSV？(y/n)");
				if (Console.ReadLine()?.Trim().ToLower() == "y")
				{
					Console.WriteLine("请输入班级列表 CSV 路径: ");
					var p = Console.ReadLine();
					if (!string.IsNullOrEmpty(p) && File.Exists(p))
					{
						var classes = CsvImporter.ImportClasses(p);
						var cstore = new ClassStore(baseDir);
						cstore.Save(classes);
						Console.WriteLine($"已导入 {classes.Count} 个班级并保存到 base/classes.json");
					}
				}

				Console.WriteLine("是否现在导入班级评价方案 CSV？(y/n)");
				if (Console.ReadLine()?.Trim().ToLower() == "y")
				{
					Console.WriteLine("请输入评价方案 CSV 路径: ");
					var p = Console.ReadLine();
					if (!string.IsNullOrEmpty(p) && File.Exists(p))
					{
						var schemes = CsvImporter.ImportScheme(p);
						var schStore = new SchemeStore(baseDir);
						schStore.Save(schemes);
						Console.WriteLine($"已导入 {schemes.Count} 条评价方案并保存到 base/schemes.json");
					}
				}

				return;
			}

			Console.WriteLine("检测到已有配置，进入登录流程。");
			// 登录流程：先输入用户密码，尝试两种方式解密 ID1
			Console.Write("请输入用户密码: ");
			var inputUser = ReadPassword();

			// 方式A：生成新的设备id并将前m位替换
			var currentDevice = DeviceIdGenerator.GenerateDeviceId();
			var m = Math.Min(inputUser.Length, currentDevice.Length);
			var deviceCandidate = inputUser + currentDevice.Substring(inputUser.Length);
			byte[] keyCandidate;
			try
			{
				var saltBytes = string.IsNullOrEmpty(model.Salt1) ? new byte[16] : Convert.FromBase64String(model.Salt1);
				keyCandidate = CryptoUtil.DeriveKey(deviceCandidate, saltBytes, 32, model?.Iterations ?? 100000);
			}
			catch
			{
				keyCandidate = CryptoUtil.DeriveKey(deviceCandidate, new byte[16], 32);
			}
			bool ok = false;
			try
			{
				if (!string.IsNullOrEmpty(model?.ID1))
				{
					var recovered = CryptoUtil.DecryptFromBase64(model.ID1, keyCandidate);
					if (recovered.StartsWith("0D0007211145141919810")) ok = true;
				}
			}
			catch { ok = false; }

			if (!ok)
			{
				// 方式B：直接以用户输入密码（填充或使用派生）作为密钥
				try
				{
					var saltB = string.IsNullOrEmpty(model?.Salt1) ? new byte[16] : Convert.FromBase64String(model.Salt1!);
					var keyB = CryptoUtil.DeriveKey(inputUser, saltB, 32, model?.Iterations ?? 100000);
					if (!string.IsNullOrEmpty(model?.ID1))
					{
						var recovered = CryptoUtil.DecryptFromBase64(model.ID1, keyB);
						if (recovered.StartsWith("0D0007211145141919810")) ok = true;
					}
				}
				catch { ok = false; }
			}

			if (!ok)
			{
				// 显示对应的密码提示（如果有）
				var hintStore = new PwhintStore(baseDir);
				var hints = hintStore.LoadHints();
				if (hints.Count > 0 && !string.IsNullOrEmpty(hints[0]))
				{
					Console.WriteLine($"密码提示: {hints[0]}");
				}
				Console.WriteLine("用户密码验证失败，请重试。");
				return;
			}

			Console.WriteLine("登录成功。");
			// 登录成功后以当前设备ID重新生成 ID1
			var newDevice = DeviceIdGenerator.GenerateDeviceId();
			var randomHex2 = DeviceIdGenerator.GenerateDeviceId().ToUpper();
			var payload2 = "0D0007211145141919810" + randomHex2;
			var newKey1 = CryptoUtil.DeriveKey(inputUser + newDevice.Substring(inputUser.Length), string.IsNullOrEmpty(model?.Salt1) ? new byte[16] : Convert.FromBase64String(model.Salt1!), 32, model?.Iterations ?? 100000);
			var newId1 = CryptoUtil.EncryptToBase64(payload2, newKey1);
			model!.ID1 = newId1;
			store.Save(model);
			Console.WriteLine("已使用当前设备ID重新生成并保存 ID1。");
		}

		static (string password, string? hint) ReadPasswordConfirmed(string label, bool requireHint=false)
		{
			string pwd;
			while (true)
			{
				Console.Write($"{label}: ");
				pwd = ReadPassword();
				if (pwd.Length < 8) { Console.WriteLine("密码至少8位。请重试。"); continue; }
				Console.Write($"确认{label}: ");
				var confirm = ReadPassword();
				if (pwd != confirm) { Console.WriteLine("两次输入不一致，请重试。"); continue; }
				string? hint = null;
				if (requireHint)
				{
					Console.Write($"{label} 提示（必填）: ");
					hint = Console.ReadLine();
				}
				return (pwd, hint);
			}
		}

		static string ReadPassword()
		{
			var pwd = string.Empty;
			ConsoleKeyInfo key;
			while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
			{
				if (key.Key == ConsoleKey.Backspace && pwd.Length > 0)
				{
					pwd = pwd[..^1];
					Console.Write("\b \b");
				}
				else if (!char.IsControl(key.KeyChar))
				{
					pwd += key.KeyChar;
					Console.Write("*");
				}
			}
			Console.WriteLine();
			return pwd;
		}
	}
}
