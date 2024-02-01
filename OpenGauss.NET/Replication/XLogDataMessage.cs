﻿using System;
using System.IO;
using OpenGauss.NET.Types;

namespace OpenGauss.NET.Replication
{

    /// <summary>
    /// A message representing a section of the WAL data stream.
    /// </summary>
    public class XLogDataMessage : ReplicationMessage
    {
        /// <summary>
        /// A section of the WAL data stream that is raw WAL data in physical replication or decoded with the selected
        /// logical decoding plugin in logical replication. It is only valid until the next <see cref="XLogDataMessage"/>
        /// is requested from the stream.
        /// </summary>
        /// <remarks>
        /// A single WAL record is never split across two XLogData messages.
        /// When a WAL record crosses a WAL page boundary, and is therefore already split using continuation records,
        /// it can be split at the page boundary. In other words, the first main WAL record and its continuation
        /// records can be sent in different XLogData messages.
        /// </remarks>
        public Stream Data { get; private set; } = default!;

        internal XLogDataMessage Populate(
            OpenGaussLogSequenceNumber walStart, OpenGaussLogSequenceNumber walEnd, DateTime serverClock, Stream data)
        {
            base.Populate(walStart, walEnd, serverClock);

            Data = data;

            return this;
        }
    }
}
