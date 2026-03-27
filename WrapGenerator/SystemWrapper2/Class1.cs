// from file:///C:/Program Files/dotnet/shared/Microsoft.NETCore.App/10.0.2/System.IO.FileSystem.DriveInfo.dll
using SystemWrapper2;
using System.IO;
using System.Runtime.Serialization;
using System;

namespace Wrapped.TestClasses
{
    public interface IDriveInfoWrap : ISerializable
    {
        DriveInfo WrappedDriveInfo { get; }
        public string Name { get; }
        public bool IsReady { get; }
        public DirectoryInfo RootDirectory { get; }
        public DriveType DriveType { get; }
        public string DriveFormat { get; }
        public long AvailableFreeSpace { get; }
        public long TotalFreeSpace { get; }
        public long TotalSize { get; }
        public string VolumeLabel { get; set; }
    }

    public class DriveInfoWrap : IDriveInfoWrap
    {
        private readonly DriveInfo inner;
        public DriveInfo WrappedDriveInfo => inner;
        public DriveInfoWrap(DriveInfo inner)
        {
            this.inner = inner;
        }
        public DriveInfoWrap(string driveName)
            : this(new DriveInfo(driveName)) { }

        public string Name
        {
            get => inner.Name;
        }
        public bool IsReady
        {
            get => inner.IsReady;
        }
        public DirectoryInfo RootDirectory
        {
            get => inner.RootDirectory;
        }
        public DriveType DriveType
        {
            get => inner.DriveType;
        }
        public string DriveFormat
        {
            get => inner.DriveFormat;
        }
        public long AvailableFreeSpace
        {
            get => inner.AvailableFreeSpace;
        }
        public long TotalFreeSpace
        {
            get => inner.TotalFreeSpace;
        }
        public long TotalSize
        {
            get => inner.TotalSize;
        }
        public string VolumeLabel
        {
            get => inner.VolumeLabel;
            set => inner.VolumeLabel = value;
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            ((ISerializable)inner).GetObjectData(info, context);
        }
        public override string ToString()
        {
            return inner.ToString();
        }
        public override bool Equals(Object obj)
        {
            return inner.Equals(obj);
        }
        public override int GetHashCode()
        {
            return inner.GetHashCode();
        }
    }

}