﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using OpenGauss;

// ReSharper disable once CheckNamespace
namespace OpenGauss.NET.Types
{
    /// <summary>
    /// A raw representation of the PostgreSQL interval datatype. Use only when <see cref="TimeSpan" /> or NodaTime
    /// <a href="https://nodatime.org/3.0.x/api/NodaTime.Period.html">Period</a> do not have sufficient range to handle your values.
    /// </summary>
    /// <remarks>
    /// <p>
    /// See https://www.postgresql.org/docs/current/static/datatype-geometric.html.
    /// </p>
    /// <p>
    /// Do not use this type unless you have to: prefer <see cref="TimeSpan" /> or NodaTime
    /// <a href="https://nodatime.org/3.0.x/api/NodaTime.Period.html">Period</a> when possible.
    /// </p>
    /// </remarks>
    public readonly struct OpenGaussInterval : IEquatable<OpenGaussInterval>
    {
        /// <summary>
        /// Constructs an <see cref="OpenGaussInterval"/>.
        /// </summary>
        public OpenGaussInterval(int months, int days, long time)
            => (Months, Days, Time) = (months, days, time);

        /// <summary>
        /// Months and years, after time for alignment.
        /// </summary>
        public int Months { get; }

        /// <summary>
        /// Days, after time for alignment.
        /// </summary>
        public int Days { get; }

        /// <summary>
        /// Remaining time unit smaller than a day, in microseconds.
        /// </summary>
        public long Time { get; }

        /// <inheritdoc />
        public bool Equals(OpenGaussInterval other)
            => Months == other.Months && Days == other.Days && Time == other.Time;

        /// <inheritdoc />
        public override bool Equals(object? obj)
            => obj is OpenGaussInterval other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
            => HashCode.Combine(Months, Days, Time);
    }
}
