﻿using OpenGauss.NET.Types;
using System;

namespace OpenGauss.NET.Replication.PgOutput.Messages
{
    /// <summary>
    /// Logical Replication Protocol begin message
    /// </summary>
    public sealed class BeginMessage : TransactionControlMessage
    {
        /// <summary>
        /// The final LSN of the transaction.
        /// </summary>
        public OpenGaussLogSequenceNumber TransactionFinalLsn { get; private set; }

        /// <summary>
        /// Commit timestamp of the transaction.
        /// The value is in number of microseconds since PostgreSQL epoch (2000-01-01).
        /// </summary>
        public DateTime TransactionCommitTimestamp { get; private set; }

        internal BeginMessage() {}

        internal BeginMessage Populate(OpenGaussLogSequenceNumber walStart, OpenGaussLogSequenceNumber walEnd, DateTime serverClock,
            OpenGaussLogSequenceNumber transactionFinalLsn, DateTime transactionCommitTimestamp, uint transactionXid)
        {
            base.Populate(walStart, walEnd, serverClock, transactionXid);
            TransactionFinalLsn = transactionFinalLsn;
            TransactionCommitTimestamp = transactionCommitTimestamp;
            return this;
        }
    }
}
