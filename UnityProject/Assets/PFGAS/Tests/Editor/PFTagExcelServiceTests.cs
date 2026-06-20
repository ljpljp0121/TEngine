using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PFGAS.Editor;

namespace PFGAS.Tests.Editor
{
    public sealed class PFTagExcelServiceTests
    {
        private string tempPath;

        [SetUp]
        public void SetUp()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "PFGAS_PFTagExcelTests");
            Directory.CreateDirectory(tempDir);
            tempPath = Path.Combine(tempDir, "PFTag_Test_" + Guid.NewGuid().ToString("N") + ".xlsx");
            File.Copy(PFTagExcelPaths.TagExcelPath, tempPath, true);
            DeleteIfExists(tempPath + ".bak");
            DeleteIfExists(tempPath + ".tmp");
        }

        [TearDown]
        public void TearDown()
        {
            DeleteIfExists(tempPath);
            DeleteIfExists(tempPath + ".bak");
            DeleteIfExists(tempPath + ".tmp");
        }

        [Test]
        public void ReadBuildsDerivedFullPathsAndValidates()
        {
            var document = new PFTagExcelService().Read(tempPath);

            Assert.That(document.Rows, Has.Count.GreaterThan(0));
            Assert.That(document.Rows.Exists(r => r.FullPath == "State.DeBuff.Fire"), Is.True);
            Assert.That(new PFTagExcelValidator().Validate(document.Rows).IsValid, Is.True);
        }

        [Test]
        public void SaveCreatesBackupAndRoundTripsRows()
        {
            var service = new PFTagExcelService();
            var document = service.Read(tempPath);
            var rows = CloneRows(document.Rows);
            rows[0].Desc = "RoundTrip";

            service.Save(document, rows);
            var reread = service.Read(tempPath);

            Assert.That(File.Exists(tempPath + ".bak"), Is.True);
            Assert.That(reread.Rows[0].Desc, Is.EqualTo("RoundTrip"));
            Assert.That(reread.Rows[0].FullPath, Is.EqualTo("State"));
        }

        [Test]
        public void SaveReportsLockedWorkbook()
        {
            var service = new PFTagExcelService();
            var document = service.Read(tempPath);

            using var stream = new FileStream(tempPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Assert.Throws<PFTagExcelFileLockedException>(() => service.Save(document, document.Rows));
        }

        [Test]
        public void ValidatorRejectsDuplicateIdsAndMissingParents()
        {
            var rows = new List<PFTagExcelRow>
            {
                new PFTagExcelRow { Id = 1, ParentId = -1, Name = "Root" },
                new PFTagExcelRow { Id = 1, ParentId = 99, Name = "Child" },
            };

            var result = new PFTagExcelValidator().Validate(rows);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FormatErrors(), Does.Contain("Tag ID 重复"));
        }

        private static List<PFTagExcelRow> CloneRows(IEnumerable<PFTagExcelRow> rows)
        {
            var result = new List<PFTagExcelRow>();
            foreach (var row in rows)
            {
                result.Add(row.Clone());
            }

            return result;
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
