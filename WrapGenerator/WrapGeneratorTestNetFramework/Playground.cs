// from file:///C:/Windows/Microsoft.NET/Framework64/v4.0.30319/mscorlib.dll
#pragma warning disable SYSLIB0050
using SystemWrapper2;
using System.IO;
using System;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using System.Security;
using System.Runtime.Remoting;
namespace Wrapped.System.IO
{
    public interface IStreamWrap : IDisposable
    {
        Stream WrappedStream { get; }
        public bool CanRead { get; }
        public bool CanSeek { get; }
        public bool CanTimeout { get; }
        public bool CanWrite { get; }
        public long Length { get; }
        public long Position { get; set; }
        public int ReadTimeout { get; set; }
        public int WriteTimeout { get; set; }
        [HostProtection(action: SecurityAction.LinkDemand, Resources = HostProtectionResource.ExternalThreading, ExternalThreading = true)]
        IAsyncResult BeginRead(Byte[] buffer, int offset, int count, AsyncCallback callback, Object state);
        [HostProtection(action: SecurityAction.LinkDemand, Resources = HostProtectionResource.ExternalThreading, ExternalThreading = true)]
        IAsyncResult BeginWrite(Byte[] buffer, int offset, int count, AsyncCallback callback, Object state);
        void Close();
        void CopyTo(IStreamWrap destination);
        void CopyTo(IStreamWrap destination, int bufferSize);
        [HostProtection(action: SecurityAction.LinkDemand, Resources = HostProtectionResource.ExternalThreading, ExternalThreading = true)]
        [ComVisible(visibility: false)]
        Task CopyToAsync(IStreamWrap destination);
        [HostProtection(action: SecurityAction.LinkDemand, Resources = HostProtectionResource.ExternalThreading, ExternalThreading = true)]
        [ComVisible(visibility: false)]
        Task CopyToAsync(IStreamWrap destination, int bufferSize);
        [HostProtection(action: SecurityAction.LinkDemand, Resources = HostProtectionResource.ExternalThreading, ExternalThreading = true)]
        [ComVisible(visibility: false)]
        Task CopyToAsync(IStreamWrap destination, int bufferSize, CancellationToken cancellationToken);
        int EndRead(IAsyncResult asyncResult);
        void EndWrite(IAsyncResult asyncResult);
        void Flush();
        [HostProtection(action: SecurityAction.LinkDemand, Resources = HostProtectionResource.ExternalThreading, ExternalThreading = true)]
        [ComVisible(visibility: false)]
        Task FlushAsync();
        [HostProtection(action: SecurityAction.LinkDemand, Resources = HostProtectionResource.ExternalThreading, ExternalThreading = true)]
        [ComVisible(visibility: false)]
        Task FlushAsync(CancellationToken cancellationToken);
        int Read(out Byte[] buffer, int offset, int count);
        [HostProtection(action: SecurityAction.LinkDemand, Resources = HostProtectionResource.ExternalThreading, ExternalThreading = true)]
        [ComVisible(visibility: false)]
        Task<int> ReadAsync(Byte[] buffer, int offset, int count);
        [HostProtection(action: SecurityAction.LinkDemand, Resources = HostProtectionResource.ExternalThreading, ExternalThreading = true)]
        [ComVisible(visibility: false)]
        Task<int> ReadAsync(Byte[] buffer, int offset, int count, CancellationToken cancellationToken);
        int ReadByte();
        long Seek(long offset, SeekOrigin origin);
        void SetLength(long value);
        void Write(Byte[] buffer, int offset, int count);
        [HostProtection(action: SecurityAction.LinkDemand, Resources = HostProtectionResource.ExternalThreading, ExternalThreading = true)]
        [ComVisible(visibility: false)]
        Task WriteAsync(Byte[] buffer, int offset, int count);
        [HostProtection(action: SecurityAction.LinkDemand, Resources = HostProtectionResource.ExternalThreading, ExternalThreading = true)]
        [ComVisible(visibility: false)]
        Task WriteAsync(Byte[] buffer, int offset, int count, CancellationToken cancellationToken);
        void WriteByte(byte value);
        [SecurityCritical(scope: SecurityCriticalScope.Explicit)]
        ObjRef CreateObjRef(Type requestedType);
        [SecurityCritical(scope: SecurityCriticalScope.Explicit)]
        Object GetLifetimeService();
        [SecurityCritical(scope: SecurityCriticalScope.Explicit)]
        Object InitializeLifetimeService();
    }

    public class StreamWrap : IStreamWrap
    {
        private readonly Stream inner;
        public Stream WrappedStream => inner;
        public StreamWrap(Stream inner)
        {
            this.inner = inner;
        }

        public bool CanRead
        {
            get => inner.CanRead;
        }
        public bool CanSeek
        {
            get => inner.CanSeek;
        }
        public bool CanTimeout
        {
            get => inner.CanTimeout;
        }
        public bool CanWrite
        {
            get => inner.CanWrite;
        }
        public long Length
        {
            get => inner.Length;
        }
        public long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }
        public int ReadTimeout
        {
            get => inner.ReadTimeout;
            set => inner.ReadTimeout = value;
        }
        public int WriteTimeout
        {
            get => inner.WriteTimeout;
            set => inner.WriteTimeout = value;
        }

        [HostProtection(action: SecurityAction.LinkDemand, Resources = HostProtectionResource.ExternalThreading, ExternalThreading = true)]
        public IAsyncResult BeginRead(Byte[] buffer, int offset, int count, AsyncCallback callback, Object state)
        {
            return inner.BeginRead(buffer, offset, count, callback, state);
        }
        [HostProtection(action: SecurityAction.LinkDemand, Resources = HostProtectionResource.ExternalThreading, ExternalThreading = true)]
        public IAsyncResult BeginWrite(Byte[] buffer, int offset, int count, AsyncCallback callback, Object state)
        {
            return inner.BeginWrite(buffer, offset, count, callback, state);
        }
        public void Close()
        {
            inner.Close();
        }
        public void CopyTo(IStreamWrap destination)
        {
            inner.CopyTo(destination.WrappedStream);
        }
        public void CopyTo(IStreamWrap destination, int bufferSize)
        {
            inner.CopyTo(destination.WrappedStream, bufferSize);
        }
        [HostProtection(action: SecurityAction.LinkDemand, Resources = HostProtectionResource.ExternalThreading, ExternalThreading = true)]
        [ComVisible(visibility: false)]
        public Task CopyToAsync(IStreamWrap destination)
        {
            return inner.CopyToAsync(destination.WrappedStream);
        }
        [HostProtection(action: SecurityAction.LinkDemand, Resources = HostProtectionResource.ExternalThreading, ExternalThreading = true)]
        [ComVisible(visibility: false)]
        public Task CopyToAsync(IStreamWrap destination, int bufferSize)
        {
            return inner.CopyToAsync(destination.WrappedStream, bufferSize);
        }
        [HostProtection(action: SecurityAction.LinkDemand, Resources = HostProtectionResource.ExternalThreading, ExternalThreading = true)]
        [ComVisible(visibility: false)]
        public Task CopyToAsync(IStreamWrap destination, int bufferSize, CancellationToken cancellationToken)
        {
            return inner.CopyToAsync(destination.WrappedStream, bufferSize, cancellationToken);
        }
        public void Dispose()
        {
            inner.Dispose();
        }
        public int EndRead(IAsyncResult asyncResult)
        {
            return inner.EndRead(asyncResult);
        }
        public void EndWrite(IAsyncResult asyncResult)
        {
            inner.EndWrite(asyncResult);
        }
        public void Flush()
        {
            inner.Flush();
        }
        [HostProtection(action: SecurityAction.LinkDemand, Resources = HostProtectionResource.ExternalThreading, ExternalThreading = true)]
        [ComVisible(visibility: false)]
        public Task FlushAsync()
        {
            return inner.FlushAsync();
        }
        [HostProtection(action: SecurityAction.LinkDemand, Resources = HostProtectionResource.ExternalThreading, ExternalThreading = true)]
        [ComVisible(visibility: false)]
        public Task FlushAsync(CancellationToken cancellationToken)
        {
            return inner.FlushAsync(cancellationToken);
        }
        public int Read(out Byte[] buffer, int offset, int count)
        {
            return inner.Read(out buffer, offset, count);
        }
        [HostProtection(action: SecurityAction.LinkDemand, Resources = HostProtectionResource.ExternalThreading, ExternalThreading = true)]
        [ComVisible(visibility: false)]
        public Task<int> ReadAsync(Byte[] buffer, int offset, int count)
        {
            return inner.ReadAsync(buffer, offset, count);
        }
        [HostProtection(action: SecurityAction.LinkDemand, Resources = HostProtectionResource.ExternalThreading, ExternalThreading = true)]
        [ComVisible(visibility: false)]
        public Task<int> ReadAsync(Byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return inner.ReadAsync(buffer, offset, count, cancellationToken);
        }
        public int ReadByte()
        {
            return inner.ReadByte();
        }
        public long Seek(long offset, SeekOrigin origin)
        {
            return inner.Seek(offset, origin);
        }
        public void SetLength(long value)
        {
            inner.SetLength(value);
        }
        public void Write(Byte[] buffer, int offset, int count)
        {
            inner.Write(buffer, offset, count);
        }
        [HostProtection(action: SecurityAction.LinkDemand, Resources = HostProtectionResource.ExternalThreading, ExternalThreading = true)]
        [ComVisible(visibility: false)]
        public Task WriteAsync(Byte[] buffer, int offset, int count)
        {
            return inner.WriteAsync(buffer, offset, count);
        }
        [HostProtection(action: SecurityAction.LinkDemand, Resources = HostProtectionResource.ExternalThreading, ExternalThreading = true)]
        [ComVisible(visibility: false)]
        public Task WriteAsync(Byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return inner.WriteAsync(buffer, offset, count, cancellationToken);
        }
        public void WriteByte(byte value)
        {
            inner.WriteByte(value);
        }
        [SecurityCritical(scope: SecurityCriticalScope.Explicit)]
        public ObjRef CreateObjRef(Type requestedType)
        {
            return inner.CreateObjRef(requestedType);
        }
        [SecurityCritical(scope: SecurityCriticalScope.Explicit)]
        public Object GetLifetimeService()
        {
            return inner.GetLifetimeService();
        }
        [SecurityCritical(scope: SecurityCriticalScope.Explicit)]
        public Object InitializeLifetimeService()
        {
            return inner.InitializeLifetimeService();
        }
        public override bool Equals(Object obj)
        {
            return inner.Equals(obj);
        }
        public override int GetHashCode()
        {
            return inner.GetHashCode();
        }
        public override string ToString()
        {
            return inner.ToString();
        }
    }

}
