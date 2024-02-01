using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;

namespace OpenGauss.NET.Internal.TypeHandlers.LTreeHandlers
{
    /// <summary>
    /// LTxtQuery binary encoding is a simple UTF8 string, but prepended with a version number.
    /// </summary>
    public class LTxtQueryHandler : TextHandler
    {
        /// <summary>
        /// Prepended to the string in the wire encoding
        /// </summary>
        const byte LTxtQueryProtocolVersion = 1;

        internal override bool PreferTextWrite => false;

        protected internal LTxtQueryHandler(PostgresType postgresType, Encoding encoding)
            : base(postgresType, encoding) {}

        #region Write

        public override int ValidateAndGetLength(string value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter) =>
            base.ValidateAndGetLength(value, ref lengthCache, parameter) + 1;


        public override int ValidateAndGetLength(char[] value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter) =>
            base.ValidateAndGetLength(value, ref lengthCache, parameter) + 1;


        public override int ValidateAndGetLength(ArraySegment<char> value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter) =>
            base.ValidateAndGetLength(value, ref lengthCache, parameter) + 1;


        public override async Task Write(string value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
        {
            if (buf.WriteSpaceLeft < 1)
                await buf.Flush(async, cancellationToken);

            buf.WriteByte(LTxtQueryProtocolVersion);
            await base.Write(value, buf, lengthCache, parameter, async, cancellationToken);
        }

        public override async Task Write(char[] value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
        {
            if (buf.WriteSpaceLeft < 1)
                await buf.Flush(async, cancellationToken);

            buf.WriteByte(LTxtQueryProtocolVersion);
            await base.Write(value, buf, lengthCache, parameter, async, cancellationToken);
        }

        public override async Task Write(ArraySegment<char> value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
        {
            if (buf.WriteSpaceLeft < 1)
                await buf.Flush(async, cancellationToken);

            buf.WriteByte(LTxtQueryProtocolVersion);
            await base.Write(value, buf, lengthCache, parameter, async, cancellationToken);
        }

        #endregion

        #region Read

        public override async ValueTask<string> Read(OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription = null)
        {
            await buf.Ensure(1, async);

            var version = buf.ReadByte();
            if (version != LTxtQueryProtocolVersion)
                throw new NotSupportedException($"Don't know how to decode ltxtquery with wire format {version}, your connection is now broken");

            return await base.Read(buf, len - 1, async, fieldDescription);
        }

        #endregion

        public override TextReader GetTextReader(Stream stream)
        {
            var version = stream.ReadByte();
            if (version != LTxtQueryProtocolVersion)
                throw new OpenGaussException($"Don't know how to decode ltxtquery with wire format {version}, your connection is now broken");

            return base.GetTextReader(stream);
        }
    }
}
