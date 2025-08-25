using System;
using System.IO;
using System.Text;
using ScoreManagerForSchool.Core.Security;
using ScoreManagerForSchool.Core.Storage;

namespace ScoreManagerForSchool.Core.Tools
{
    /// <summary>
    /// 密钥恢复和数据修复工具
    /// </summary>
    public static class CryptoRecoveryTool
    {
        /// <summary>
        /// 尝试恢复损坏的加密数据
        /// </summary>
        public static bool TryRecoverEncryptedFile(string filePath, string backupPath = "")
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"文件不存在: {filePath}");
                    return false;
                }

                var encryptedData = File.ReadAllText(filePath, Encoding.UTF8);
                
                // 诊断加密数据
                var diagnosis = CryptoUtil.DiagnoseEncryptedData(encryptedData);
                Console.WriteLine($"文件诊断结果:\n{diagnosis}");
                
                // 获取环境诊断
                var envDiag = KeyManager.GetEnvironmentDiagnostics();
                Console.WriteLine($"\n{envDiag}");
                
                // 尝试不同的恢复策略
                if (TryRecoverWithCurrentEnvironment(encryptedData))
                {
                    Console.WriteLine("✅ 使用当前环境成功恢复数据");
                    return true;
                }
                
                if (!string.IsNullOrEmpty(backupPath) && TryRecoverFromBackup(filePath, backupPath))
                {
                    Console.WriteLine("✅ 从备份成功恢复数据");
                    return true;
                }
                
                Console.WriteLine("❌ 数据恢复失败");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 恢复过程中发生错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 使用当前环境尝试恢复
        /// </summary>
        private static bool TryRecoverWithCurrentEnvironment(string encryptedData)
        {
            try
            {
                var key = KeyManager.GetDataEncryptionKey(Environment.CurrentDirectory);
                if (key == null) return false;
                
                var decrypted = CryptoUtil.SafeDecryptFromBase64(encryptedData, key, true);
                return !string.IsNullOrEmpty(decrypted);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 从备份恢复
        /// </summary>
        private static bool TryRecoverFromBackup(string originalPath, string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath)) return false;
                
                // 创建原文件的备份
                var corruptedBackup = originalPath + ".corrupted." + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.Copy(originalPath, corruptedBackup);
                
                // 恢复备份文件
                File.Copy(backupPath, originalPath, true);
                
                Console.WriteLine($"已将损坏文件备份为: {corruptedBackup}");
                Console.WriteLine($"已从备份恢复: {backupPath} -> {originalPath}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"备份恢复失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 创建数据文件备份
        /// </summary>
        public static bool CreateBackup(string baseDir)
        {
            try
            {
                var backupDir = Path.Combine(baseDir, "backup", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                Directory.CreateDirectory(backupDir);
                
                var filesToBackup = new[]
                {
                    "master.key",
                    "students.json.enc",
                    "evaluations.json.enc",
                    "classes.json.enc",
                    "teachers.json.enc",
                    "schemes.json.enc"
                };

                int backedUpCount = 0;
                foreach (var fileName in filesToBackup)
                {
                    var sourcePath = Path.Combine(baseDir, fileName);
                    var targetPath = Path.Combine(backupDir, fileName);
                    
                    if (File.Exists(sourcePath))
                    {
                        File.Copy(sourcePath, targetPath);
                        backedUpCount++;
                        Console.WriteLine($"✅ 已备份: {fileName}");
                    }
                }
                
                if (backedUpCount > 0)
                {
                    Console.WriteLine($"✅ 备份完成，共备份 {backedUpCount} 个文件到: {backupDir}");
                    return true;
                }
                else
                {
                    Console.WriteLine("⚠️ 没有找到需要备份的文件");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 备份失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 重置加密系统（删除所有加密文件和密钥）
        /// </summary>
        public static bool ResetEncryptionSystem(string baseDir, bool createBackup = true)
        {
            try
            {
                if (createBackup)
                {
                    Console.WriteLine("🔄 创建重置前备份...");
                    CreateBackup(baseDir);
                }

                var filesToDelete = new[]
                {
                    "master.key",
                    "students.json.enc",
                    "evaluations.json.enc", 
                    "classes.json.enc",
                    "teachers.json.enc",
                    "schemes.json.enc"
                };

                int deletedCount = 0;
                foreach (var fileName in filesToDelete)
                {
                    var filePath = Path.Combine(baseDir, fileName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        deletedCount++;
                        Console.WriteLine($"🗑️ 已删除: {fileName}");
                    }
                }

                Console.WriteLine($"✅ 加密系统重置完成，删除了 {deletedCount} 个文件");
                Console.WriteLine("ℹ️ 下次启动时将自动生成新的密钥");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 重置失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 生成加密系统健康报告
        /// </summary>
        public static string GenerateHealthReport(string baseDir)
        {
            var report = new StringBuilder();
            report.AppendLine("🏥 加密系统健康检查报告");
            report.AppendLine($"📅 检查时间: {DateTime.Now}");
            report.AppendLine($"📁 基础目录: {baseDir}");
            report.AppendLine();

            // 环境信息
            report.AppendLine(KeyManager.GetEnvironmentDiagnostics());
            report.AppendLine();

            // 文件状态检查
            var filesToCheck = new[]
            {
                ("master.key", "主密钥文件"),
                ("students.json.enc", "学生数据"),
                ("evaluations.json.enc", "评价数据"),
                ("classes.json.enc", "班级数据"),
                ("teachers.json.enc", "教师数据"),
                ("schemes.json.enc", "方案数据")
            };

            report.AppendLine("📋 文件状态检查:");
            foreach (var (fileName, description) in filesToCheck)
            {
                var filePath = Path.Combine(baseDir, fileName);
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    report.AppendLine($"✅ {description} ({fileName}): {fileInfo.Length} 字节, 修改时间: {fileInfo.LastWriteTime}");
                    
                    if (fileName.EndsWith(".enc"))
                    {
                        try
                        {
                            var content = File.ReadAllText(filePath);
                            var diagnosis = CryptoUtil.DiagnoseEncryptedData(content);
                            report.AppendLine($"   🔍 {diagnosis.Replace("\n", "\n   ")}");
                        }
                        catch (Exception ex)
                        {
                            report.AppendLine($"   ❌ 诊断失败: {ex.Message}");
                        }
                    }
                }
                else
                {
                    report.AppendLine($"❌ {description} ({fileName}): 文件不存在");
                }
            }

            return report.ToString();
        }
    }
}
