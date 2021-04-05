using System;
using System.Collections.Generic;
using System.IO;

namespace MBW.Tools.ElephantProject.Helpers
{
    class FileInfoComparer : IEqualityComparer<FileInfo>
    {
        public static FileInfoComparer Instance { get; } = new();

        private FileInfoComparer()
        {
        }

        public bool Equals(FileInfo x, FileInfo y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;

            return string.Equals(x.FullName, y.FullName, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(FileInfo obj)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name);
        }
    }
}