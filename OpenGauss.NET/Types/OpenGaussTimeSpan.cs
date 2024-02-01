﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using OpenGauss;

// ReSharper disable once CheckNamespace
namespace OpenGauss.NET.Types
{
    /// <summary>
    /// Represents the PostgreSQL interval datatype.
    /// </summary>
    /// <remarks>PostgreSQL differs from .NET in how it's interval type doesn't assume 24 hours in a day
    /// (to deal with 23- and 25-hour days caused by daylight savings adjustments) and has a concept
    /// of months that doesn't exist in .NET's <see cref="TimeSpan"/> class. (Neither datatype
    /// has any concessions for leap-seconds).
    /// <para>For most uses just casting to and from TimeSpan will work correctly &#x2014; in particular,
    /// the results of subtracting one <see cref="DateTime"/> or the PostgreSQL date, time and
    /// timestamp types from another should be the same whether you do so in .NET or PostgreSQL &#x2014;
    /// but if the handling of days and months in PostgreSQL is important to your application then you
    /// should use this class instead of <see cref="TimeSpan"/>.</para>
    /// <para>If you don't know whether these differences are important to your application, they
    /// probably arent! Just use <see cref="TimeSpan"/> and do not use this class directly &#x263a;</para>
    /// <para>To avoid forcing unnecessary provider-specific concerns on users who need not be concerned
    /// with them a call to <see cref="System.Data.IDataRecord.GetValue(int)"/> on a field containing an
    /// <see cref="OpenGaussTimeSpan"/> value will return a <see cref="TimeSpan"/> rather than an
    /// <see cref="OpenGaussTimeSpan"/>. If you need the extra functionality of <see cref="OpenGaussTimeSpan"/>
    /// then use <see cref="OpenGaussDataReader.GetInterval(int)"/>.</para>
    /// </remarks>
    /// <seealso cref="Ticks"/>
    /// <seealso cref="JustifyDays"/>
    /// <seealso cref="JustifyMonths"/>
    /// <seealso cref="Canonicalize()"/>
    [Obsolete(
        "For values outside the range of TimeSpan, consider using NodaTime (range -9998 to 9999), or read the value as an OpenGaussInterval. " +
        "See https://www.opengauss.org/doc/types/datetime.html for more information.")]
    [Serializable]
    public readonly struct OpenGaussTimeSpan : IComparable, IComparer, IEquatable<OpenGaussTimeSpan>, IComparable<OpenGaussTimeSpan>,
                                   IComparer<OpenGaussTimeSpan>
    {
        #region Constants

        /// <summary>
        /// Represents the number of ticks (100ns periods) in one microsecond. This field is constant.
        /// </summary>
        public const long TicksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1000;

        /// <summary>
        /// Represents the number of ticks (100ns periods) in one millisecond. This field is constant.
        /// </summary>
        public const long TicksPerMillsecond = TimeSpan.TicksPerMillisecond;

        /// <summary>
        /// Represents the number of ticks (100ns periods) in one second. This field is constant.
        /// </summary>
        public const long TicksPerSecond = TimeSpan.TicksPerSecond;

        /// <summary>
        /// Represents the number of ticks (100ns periods) in one minute. This field is constant.
        /// </summary>
        public const long TicksPerMinute = TimeSpan.TicksPerMinute;

        /// <summary>
        /// Represents the number of ticks (100ns periods) in one hour. This field is constant.
        /// </summary>
        public const long TicksPerHour = TimeSpan.TicksPerHour;

        /// <summary>
        /// Represents the number of ticks (100ns periods) in one day. This field is constant.
        /// </summary>
        public const long TicksPerDay = TimeSpan.TicksPerDay;

        /// <summary>
        /// Represents the number of hours in one day (assuming no daylight savings adjustments). This field is constant.
        /// </summary>
        public const int HoursPerDay = 24;

        /// <summary>
        /// Represents the number of days assumed in one month if month justification or unjustifcation is performed.
        /// This is set to 30 for consistency with PostgreSQL. Note that this is means that month adjustments cause
        /// a year to be taken as 30 &#xd7; 12 = 360 rather than 356/366 days.
        /// </summary>
        public const int DaysPerMonth = 30;

        /// <summary>
        /// Represents the number of ticks (100ns periods) in one day, assuming 30 days per month. <seealso cref="DaysPerMonth"/>
        /// </summary>
        public const long TicksPerMonth = TicksPerDay * DaysPerMonth;

        /// <summary>
        /// Represents the number of months in a year. This field is constant.
        /// </summary>
        public const int MonthsPerYear = 12;

        /// <summary>
        /// Represents the maximum <see cref="OpenGaussTimeSpan"/>. This field is read-only.
        /// </summary>
        public static readonly OpenGaussTimeSpan MaxValue = new(long.MaxValue);

        /// <summary>
        /// Represents the minimum <see cref="OpenGaussTimeSpan"/>. This field is read-only.
        /// </summary>
        public static readonly OpenGaussTimeSpan MinValue = new(long.MinValue);

        /// <summary>
        /// Represents the zero <see cref="OpenGaussTimeSpan"/>. This field is read-only.
        /// </summary>
        public static readonly OpenGaussTimeSpan Zero = new(0);

        #endregion

        readonly int _months;
        readonly int _days;
        readonly long _ticks;

        #region Constructors

        /// <summary>
        /// Initializes a new <see cref="OpenGaussTimeSpan"/> to the specified number of ticks.
        /// </summary>
        /// <param name="ticks">A time period expressed in 100ns units.</param>
        public OpenGaussTimeSpan(long ticks)
            : this(new TimeSpan(ticks))
        {
        }

        /// <summary>
        /// Initializes a new <see cref="OpenGaussTimeSpan"/> to hold the same time as a <see cref="TimeSpan"/>
        /// </summary>
        /// <param name="timespan">A time period expressed in a <see cref="TimeSpan"/></param>
        public OpenGaussTimeSpan(TimeSpan timespan)
            : this(0, timespan.Days, timespan.Ticks - (TicksPerDay * timespan.Days))
        {
        }

        /// <summary>
        /// Initializes a new <see cref="OpenGaussTimeSpan"/> to the specified number of months, days
        /// &amp; ticks.
        /// </summary>
        /// <param name="months">Number of months.</param>
        /// <param name="days">Number of days.</param>
        /// <param name="ticks">Number of 100ns units.</param>
        public OpenGaussTimeSpan(int months, int days, long ticks)
        {
            _months = months;
            _days = days;
            _ticks = ticks;
        }

        /// <summary>
        /// Initializes a new <see cref="OpenGaussTimeSpan"/> to the specified number of
        /// days, hours, minutes &amp; seconds.
        /// </summary>
        /// <param name="days">Number of days.</param>
        /// <param name="hours">Number of hours.</param>
        /// <param name="minutes">Number of minutes.</param>
        /// <param name="seconds">Number of seconds.</param>
        public OpenGaussTimeSpan(int days, int hours, int minutes, int seconds)
            : this(0, days, new TimeSpan(hours, minutes, seconds).Ticks)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="OpenGaussTimeSpan"/> to the specified number of
        /// days, hours, minutes, seconds &amp; milliseconds.
        /// </summary>
        /// <param name="days">Number of days.</param>
        /// <param name="hours">Number of hours.</param>
        /// <param name="minutes">Number of minutes.</param>
        /// <param name="seconds">Number of seconds.</param>
        /// <param name="milliseconds">Number of milliseconds.</param>
        public OpenGaussTimeSpan(int days, int hours, int minutes, int seconds, int milliseconds)
            : this(0, days, new TimeSpan(0, hours, minutes, seconds, milliseconds).Ticks)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="OpenGaussTimeSpan"/> to the specified number of
        /// months, days, hours, minutes, seconds &amp; milliseconds.
        /// </summary>
        /// <param name="months">Number of months.</param>
        /// <param name="days">Number of days.</param>
        /// <param name="hours">Number of hours.</param>
        /// <param name="minutes">Number of minutes.</param>
        /// <param name="seconds">Number of seconds.</param>
        /// <param name="milliseconds">Number of milliseconds.</param>
        public OpenGaussTimeSpan(int months, int days, int hours, int minutes, int seconds, int milliseconds)
            : this(months, days, new TimeSpan(0, hours, minutes, seconds, milliseconds).Ticks)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="OpenGaussTimeSpan"/> to the specified number of
        /// years, months, days, hours, minutes, seconds &amp; milliseconds.
        /// <para>Years are calculated exactly equivalent to 12 months.</para>
        /// </summary>
        /// <param name="years">Number of years.</param>
        /// <param name="months">Number of months.</param>
        /// <param name="days">Number of days.</param>
        /// <param name="hours">Number of hours.</param>
        /// <param name="minutes">Number of minutes.</param>
        /// <param name="seconds">Number of seconds.</param>
        /// <param name="milliseconds">Number of milliseconds.</param>
        public OpenGaussTimeSpan(int years, int months, int days, int hours, int minutes, int seconds, int milliseconds)
            : this(years * 12 + months, days, new TimeSpan(0, hours, minutes, seconds, milliseconds).Ticks)
        {
        }

        #endregion

        #region Whole Parts

        /// <summary>
        /// The total number of ticks(100ns units) contained. This is the resolution of the
        /// <see cref="OpenGaussTimeSpan"/>  type. This ignores the number of days and
        /// months held. If you want them included use <see cref="UnjustifyInterval()"/> first.
        /// <remarks>The resolution of the PostgreSQL
        /// interval type is by default 1&#xb5;s = 1,000 ns. It may be smaller as follows:
        /// <list type="number">
        /// <item>
        /// <term>interval(0)</term>
        /// <description>resolution of 1s (1 second)</description>
        /// </item>
        /// <item>
        /// <term>interval(1)</term>
        /// <description>resolution of 100ms = 0.1s (100 milliseconds)</description>
        /// </item>
        /// <item>
        /// <term>interval(2)</term>
        /// <description>resolution of 10ms = 0.01s (10 milliseconds)</description>
        /// </item>
        /// <item>
        /// <term>interval(3)</term>
        /// <description>resolution of 1ms = 0.001s (1 millisecond)</description>
        /// </item>
        /// <item>
        /// <term>interval(4)</term>
        /// <description>resolution of 100&#xb5;s = 0.0001s (100 microseconds)</description>
        /// </item>
        /// <item>
        /// <term>interval(5)</term>
        /// <description>resolution of 10&#xb5;s = 0.00001s (10 microseconds)</description>
        /// </item>
        /// <item>
        /// <term>interval(6) or interval</term>
        /// <description>resolution of 1&#xb5;s = 0.000001s (1 microsecond)</description>
        /// </item>
        /// </list>
        /// <para>As such, if the 100-nanosecond resolution is significant to an application, a PostgreSQL interval will
        /// not suffice for those purposes.</para>
        /// <para>In more frequent cases though, the resolution of the interval suffices.
        /// <see cref="OpenGaussTimeSpan"/> will always suffice to handle the resolution of any interval value, and upon
        /// writing to the database, will be rounded to the resolution used.</para>
        /// </remarks>
        /// <returns>The number of ticks in the instance.</returns>
        /// </summary>
        public long Ticks => _ticks;

        /// <summary>
        /// Gets the number of whole microseconds held in the instance.
        /// <returns>An  in the range [-999999, 999999].</returns>
        /// </summary>
        public int Microseconds => (int)((_ticks / 10) % 1000000);

        /// <summary>
        /// Gets the number of whole milliseconds held in the instance.
        /// <returns>An  in the range [-999, 999].</returns>
        /// </summary>
        public int Milliseconds => (int)((_ticks / TicksPerMillsecond) % 1000);

        /// <summary>
        /// Gets the number of whole seconds held in the instance.
        /// <returns>An  in the range [-59, 59].</returns>
        /// </summary>
        public int Seconds => (int)((_ticks / TicksPerSecond) % 60);

        /// <summary>
        /// Gets the number of whole minutes held in the instance.
        /// <returns>An  in the range [-59, 59].</returns>
        /// </summary>
        public int Minutes => (int)((_ticks / TicksPerMinute) % 60);

        /// <summary>
        /// Gets the number of whole hours held in the instance.
        /// <remarks>Note that this can be less than -23 or greater than 23 unless <see cref="JustifyDays()"/>
        /// has been used to produce this instance.</remarks>
        /// </summary>
        public int Hours => (int)(_ticks / TicksPerHour);

        /// <summary>
        /// Gets the number of days held in the instance.
        /// <remarks>Note that this does not pay attention to a time component with -24 or less hours or
        /// 24 or more hours, unless <see cref="JustifyDays()"/> has been called to produce this instance.</remarks>
        /// </summary>
        public int Days => _days;

        /// <summary>
        /// Gets the number of months held in the instance.
        /// <remarks>Note that this does not pay attention to a day component with -30 or less days or
        /// 30 or more days, unless <see cref="JustifyMonths()"/> has been called to produce this instance.</remarks>
        /// </summary>
        public int Months => _months;

        /// <summary>
        /// Returns a <see cref="TimeSpan"/> representing the time component of the instance.
        /// <remarks>Note that this may have a value beyond the range &#xb1;23:59:59.9999999 unless
        /// <see cref="JustifyDays()"/> has been called to produce this instance.</remarks>
        /// </summary>
        public TimeSpan Time => new(_ticks);

        #endregion

        #region Total Parts

        /// <summary>
        /// The total number of ticks (100ns units) in the instance, assuming 24 hours in each day and
        /// 30 days in a month.
        /// </summary>
        public long TotalTicks => Ticks + Days * TicksPerDay + Months * TicksPerMonth;

        /// <summary>
        /// The total number of microseconds in the instance, assuming 24 hours in each day and
        /// 30 days in a month.
        /// </summary>
        public double TotalMicroseconds => TotalTicks / 10d;

        /// <summary>
        /// The total number of milliseconds in the instance, assuming 24 hours in each day and
        /// 30 days in a month.
        /// </summary>
        public double TotalMilliseconds => TotalTicks / (double)TicksPerMillsecond;

        /// <summary>
        /// The total number of seconds in the instance, assuming 24 hours in each day and
        /// 30 days in a month.
        /// </summary>
        public double TotalSeconds => TotalTicks / (double)TicksPerSecond;

        /// <summary>
        /// The total number of minutes in the instance, assuming 24 hours in each day and
        /// 30 days in a month.
        /// </summary>
        public double TotalMinutes => TotalTicks / (double)TicksPerMinute;

        /// <summary>
        /// The total number of hours in the instance, assuming 24 hours in each day and
        /// 30 days in a month.
        /// </summary>
        public double TotalHours => TotalTicks / (double)TicksPerHour;

        /// <summary>
        /// The total number of days in the instance, assuming 24 hours in each day and
        /// 30 days in a month.
        /// </summary>
        public double TotalDays => TotalTicks / (double)TicksPerDay;

        /// <summary>
        /// The total number of months in the instance, assuming 24 hours in each day and
        /// 30 days in a month.
        /// </summary>
        public double TotalMonths => TotalTicks / (double)TicksPerMonth;

        #endregion

        #region Create From Part

        /// <summary>
        /// Creates an <see cref="OpenGaussTimeSpan"/> from a number of ticks.
        /// </summary>
        /// <param name="ticks">The number of ticks (100ns units) in the interval.</param>
        /// <returns>A <see cref="Canonicalize()"/>d <see cref="OpenGaussTimeSpan"/> with the given number of ticks.</returns>
        public static OpenGaussTimeSpan FromTicks(long ticks) => new OpenGaussTimeSpan(ticks).Canonicalize();

        /// <summary>
        /// Creates an <see cref="OpenGaussTimeSpan"/> from a number of microseconds.
        /// </summary>
        /// <param name="micro">The number of microseconds in the interval.</param>
        /// <returns>A <see cref="Canonicalize()"/>d <see cref="OpenGaussTimeSpan"/> with the given number of microseconds.</returns>
        public static OpenGaussTimeSpan FromMicroseconds(double micro) => FromTicks((long)(micro * TicksPerMicrosecond));

        /// <summary>
        /// Creates an <see cref="OpenGaussTimeSpan"/> from a number of milliseconds.
        /// </summary>
        /// <param name="milli">The number of milliseconds in the interval.</param>
        /// <returns>A <see cref="Canonicalize()"/>d <see cref="OpenGaussTimeSpan"/> with the given number of milliseconds.</returns>
        public static OpenGaussTimeSpan FromMilliseconds(double milli) => FromTicks((long)(milli * TicksPerMillsecond));

        /// <summary>
        /// Creates an <see cref="OpenGaussTimeSpan"/> from a number of seconds.
        /// </summary>
        /// <param name="seconds">The number of seconds in the interval.</param>
        /// <returns>A <see cref="Canonicalize()"/>d <see cref="OpenGaussTimeSpan"/> with the given number of seconds.</returns>
        public static OpenGaussTimeSpan FromSeconds(double seconds) => FromTicks((long)(seconds * TicksPerSecond));

        /// <summary>
        /// Creates an <see cref="OpenGaussTimeSpan"/> from a number of minutes.
        /// </summary>
        /// <param name="minutes">The number of minutes in the interval.</param>
        /// <returns>A <see cref="Canonicalize()"/>d <see cref="OpenGaussTimeSpan"/> with the given number of minutes.</returns>
        public static OpenGaussTimeSpan FromMinutes(double minutes) => FromTicks((long)(minutes * TicksPerMinute));

        /// <summary>
        /// Creates an <see cref="OpenGaussTimeSpan"/> from a number of hours.
        /// </summary>
        /// <param name="hours">The number of hours in the interval.</param>
        /// <returns>A <see cref="Canonicalize()"/>d <see cref="OpenGaussTimeSpan"/> with the given number of hours.</returns>
        public static OpenGaussTimeSpan FromHours(double hours) => FromTicks((long)(hours * TicksPerHour));

        /// <summary>
        /// Creates an <see cref="OpenGaussTimeSpan"/> from a number of days.
        /// </summary>
        /// <param name="days">The number of days in the interval.</param>
        /// <returns>A <see cref="Canonicalize()"/>d <see cref="OpenGaussTimeSpan"/> with the given number of days.</returns>
        public static OpenGaussTimeSpan FromDays(double days) => FromTicks((long)(days * TicksPerDay));

        /// <summary>
        /// Creates an <see cref="OpenGaussTimeSpan"/> from a number of months.
        /// </summary>
        /// <param name="months">The number of months in the interval.</param>
        /// <returns>A <see cref="Canonicalize()"/>d <see cref="OpenGaussTimeSpan"/> with the given number of months.</returns>
        public static OpenGaussTimeSpan FromMonths(double months) => FromTicks((long)(months * TicksPerMonth));

        #endregion

        #region Arithmetic

        /// <summary>
        /// Adds another interval to this instance and returns the result.
        /// </summary>
        /// <param name="interval">An <see cref="OpenGaussTimeSpan"/> to add to this instance.</param>
        /// <returns>An <see cref="OpenGaussTimeSpan"/> whose values are the sums of the two instances.</returns>
        public OpenGaussTimeSpan Add(in OpenGaussTimeSpan interval)
            => new(Months + interval.Months, Days + interval.Days, Ticks + interval.Ticks);

        /// <summary>
        /// Subtracts another interval from this instance and returns the result.
        /// </summary>
        /// <param name="interval">An <see cref="OpenGaussTimeSpan"/> to subtract from this instance.</param>
        /// <returns>An <see cref="OpenGaussTimeSpan"/> whose values are the differences of the two instances.</returns>
        public OpenGaussTimeSpan Subtract(in OpenGaussTimeSpan interval)
            => new(Months - interval.Months, Days - interval.Days, Ticks - interval.Ticks);

        /// <summary>
        /// Returns an <see cref="OpenGaussTimeSpan"/> whose value is the negated value of this instance.
        /// </summary>
        /// <returns>An <see cref="OpenGaussTimeSpan"/> whose value is the negated value of this instance.</returns>
        public OpenGaussTimeSpan Negate() => new(-Months, -Days, -Ticks);

        /// <summary>
        /// This absolute value of this instance. In the case of some, but not all, components being negative,
        /// the rules used for justification are used to determine if the instance is positive or negative.
        /// </summary>
        /// <returns>An <see cref="OpenGaussTimeSpan"/> whose value is the absolute value of this instance.</returns>
        public OpenGaussTimeSpan Duration()
            => UnjustifyInterval().Ticks < 0 ? Negate() : this;

        #endregion

        #region Justification

        /// <summary>
        /// Equivalent to PostgreSQL's justify_days function.
        /// </summary>
        /// <returns>An <see cref="OpenGaussTimeSpan"/> based on this one, but with any hours outside of the range [-23, 23]
        /// converted into days.</returns>
        public OpenGaussTimeSpan JustifyDays()
        {
            return new(Months, Days + (int)(Ticks / TicksPerDay), Ticks % TicksPerDay);
        }

        /// <summary>
        /// Opposite to PostgreSQL's justify_days function.
        /// </summary>
        /// <returns>An <see cref="OpenGaussTimeSpan"/> based on this one, but with any days converted to multiples of &#xB1;24hours.</returns>
        public OpenGaussTimeSpan UnjustifyDays()
        {
            return new(Months, 0, Ticks + Days * TicksPerDay);
        }

        /// <summary>
        /// Equivalent to PostgreSQL's justify_months function.
        /// </summary>
        /// <returns>An <see cref="OpenGaussTimeSpan"/> based on this one, but with any days outside of the range [-30, 30]
        /// converted into months.</returns>
        public OpenGaussTimeSpan JustifyMonths()
        {
            return new(Months + Days / DaysPerMonth, Days % DaysPerMonth, Ticks);
        }

        /// <summary>
        /// Opposite to PostgreSQL's justify_months function.
        /// </summary>
        /// <returns>An <see cref="OpenGaussTimeSpan"/> based on this one, but with any months converted to multiples of &#xB1;30days.</returns>
        public OpenGaussTimeSpan UnjustifyMonths()
        {
            return new(0, Days + Months * DaysPerMonth, Ticks);
        }

        /// <summary>
        /// Equivalent to PostgreSQL's justify_interval function.
        /// </summary>
        /// <returns>An <see cref="OpenGaussTimeSpan"/> based on this one,
        /// but with any months converted to multiples of &#xB1;30days
        /// and then with any days converted to multiples of &#xB1;24hours</returns>
        public OpenGaussTimeSpan JustifyInterval()
        {
            return JustifyMonths().JustifyDays();
        }

        /// <summary>
        /// Opposite to PostgreSQL's justify_interval function.
        /// </summary>
        /// <returns>An <see cref="OpenGaussTimeSpan"/> based on this one, but with any months converted to multiples of &#xB1;30days and then any days converted to multiples of &#xB1;24hours;</returns>
        public OpenGaussTimeSpan UnjustifyInterval()
        {
            return new(Ticks + Days * TicksPerDay + Months * DaysPerMonth * TicksPerDay);
        }

        /// <summary>
        /// Produces a canonical NpgslInterval with 0 months and hours in the range of [-23, 23].
        /// <remarks>
        /// <para>
        /// While the fact that for many purposes, two different <see cref="OpenGaussTimeSpan"/> instances could be considered
        /// equivalent (e.g. one with 2days, 3hours and one with 1day 27hours) there are different possible canonical forms.
        /// </para><para>
        /// E.g. we could move all excess hours into days and all excess days into months and have the most readable form,
        /// or we could move everything into the ticks and have the form that allows for the easiest arithmetic) the form
        /// chosen has two important properties that make it the best choice.
        /// </para><para>First, it is closest two how
        /// <see cref="TimeSpan"/> objects are most often represented. Second, it is compatible with results of many
        /// PostgreSQL functions, particularly with age() and the results of subtracting one date, time or timestamp from
        /// another.
        /// </para>
        /// <para>Note that the results of casting a <see cref="TimeSpan"/> to <see cref="OpenGaussTimeSpan"/> is
        /// canonicalised.</para>
        /// </remarks>
        /// </summary>
        /// <returns>An <see cref="OpenGaussTimeSpan"/> based on this one, but with months converted to multiples of &#xB1;30days and with any hours outside of the range [-23, 23]
        /// converted into days.</returns>
        public OpenGaussTimeSpan Canonicalize()
        {
            return new(0, Days + Months * DaysPerMonth + (int)(Ticks / TicksPerDay), Ticks % TicksPerDay);
        }

        #endregion

        #region Casts

        /// <summary>
        /// Implicit cast of a <see cref="TimeSpan"/> to an <see cref="OpenGaussTimeSpan"/>
        /// </summary>
        /// <param name="timespan">A <see cref="TimeSpan"/></param>
        /// <returns>An eqivalent, canonical, <see cref="OpenGaussTimeSpan"/>.</returns>
        public static implicit operator OpenGaussTimeSpan(TimeSpan timespan) => ToOpenGaussTimeSpan(timespan);

        /// <summary>
        /// Casts a <see cref="TimeSpan"/> to an <see cref="OpenGaussTimeSpan"/>.
        /// </summary>
        public static OpenGaussTimeSpan ToOpenGaussTimeSpan(TimeSpan timespan) => new OpenGaussTimeSpan(timespan).Canonicalize();

        /// <summary>
        /// Explicit cast of an <see cref="OpenGaussTimeSpan"/> to a <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="interval">A <see cref="OpenGaussTimeSpan"/>.</param>
        /// <returns>An equivalent <see cref="TimeSpan"/>.</returns>
        public static explicit operator TimeSpan(OpenGaussTimeSpan interval)
            => ToTimeSpan(interval);

        /// <summary>
        /// Casts an <see cref="OpenGaussTimeSpan"/> to a <see cref="TimeSpan"/>.
        /// </summary>
        public static TimeSpan ToTimeSpan(in OpenGaussTimeSpan interval)
            => interval.Months > 0
                ? throw new InvalidCastException("Cannot convert interval value with non-zero months to TimeSpan")
                : new(interval.Ticks + interval.Days * TicksPerDay);

        #endregion

        #region Comparison

        /// <summary>
        /// Returns true if another <see cref="OpenGaussTimeSpan"/> is exactly the same as this instance.
        /// </summary>
        /// <param name="other">An <see cref="OpenGaussTimeSpan"/> for comparison.</param>
        /// <returns>true if the two <see cref="OpenGaussTimeSpan"/> instances are exactly the same,
        /// false otherwise.</returns>
        public bool Equals(OpenGaussTimeSpan other)
            => Ticks == other.Ticks && Days == other.Days && Months == other.Months;

        /// <summary>
        /// Returns true if another object is an <see cref="OpenGaussTimeSpan"/>, that is exactly the same as
        /// this instance
        /// </summary>
        /// <param name="obj">An <see cref="object"/> for comparison.</param>
        /// <returns>true if the argument is an <see cref="OpenGaussTimeSpan"/> and is exactly the same
        /// as this one, false otherwise.</returns>
        public override bool Equals(object? obj) => obj is OpenGaussTimeSpan span && Equals(span);

        /// <summary>
        /// Compares two <see cref="OpenGaussTimeSpan"/> instances.
        /// </summary>
        /// <param name="x">The first <see cref="OpenGaussTimeSpan"/>.</param>
        /// <param name="y">The second <see cref="OpenGaussTimeSpan"/>.</param>
        /// <returns>0 if the two are equal or equivalent. A value greater than zero if x is greater than y,
        /// a value less than zero if x is less than y.</returns>
        public static int Compare(OpenGaussTimeSpan x, OpenGaussTimeSpan y) => x.CompareTo(y);

        int IComparer<OpenGaussTimeSpan>.Compare(OpenGaussTimeSpan x, OpenGaussTimeSpan y) => x.CompareTo(y);

        int IComparer.Compare(object? x, object? y)
        {
            if (x == null)
                return y == null ? 0 : 1;
            if (y == null)
                return -1;
            try {
                return ((IComparable)x).CompareTo(y);
            } catch (Exception) {
                throw new ArgumentException();
            }
        }

        /// <summary>
        /// A hash code suitable for uses with hashing algorithms.
        /// </summary>
        /// <returns>An signed integer.</returns>
        public override int GetHashCode() => UnjustifyInterval().Ticks.GetHashCode();

        /// <summary>
        /// Compares this instance with another/
        /// </summary>
        /// <param name="other">An <see cref="OpenGaussTimeSpan"/> to compare this with.</param>
        /// <returns>0 if the instances are equal or equivalent. A value less than zero if
        /// this instance is less than the argument. A value greater than zero if this instance
        /// is greater than the instance.</returns>
        public int CompareTo(OpenGaussTimeSpan other)
            => UnjustifyInterval().Ticks.CompareTo(other.UnjustifyInterval().Ticks);

        /// <summary>
        /// Compares this instance with another/
        /// </summary>
        /// <param name="other">An object to compare this with.</param>
        /// <returns>0 if the argument is an <see cref="OpenGaussTimeSpan"/> and the instances are equal or equivalent.
        /// A value less than zero if the argument is an <see cref="OpenGaussTimeSpan"/> and
        /// this instance is less than the argument.
        /// A value greater than zero if the argument is an <see cref="OpenGaussTimeSpan"/> and this instance
        /// is greater than the instance.</returns>
        /// A value greater than zero if the argument is null.
        /// <exception cref="ArgumentException">The argument is not an <see cref="OpenGaussTimeSpan"/>.</exception>
        public int CompareTo(object? other)
        {
            if (other == null)
                return 1;
            if (other is OpenGaussTimeSpan)
                return CompareTo((OpenGaussTimeSpan)other);
            throw new ArgumentException(nameof(other));
        }

        #endregion

        #region String Conversions

        /// <summary>
        /// Parses a <see cref="string"/> and returns a <see cref="OpenGaussTimeSpan"/> instance.
        /// Designed to use the formats generally returned by PostgreSQL.
        /// </summary>
        /// <param name="str">The <see cref="string"/> to parse.</param>
        /// <returns>An <see cref="OpenGaussTimeSpan"/> represented by the argument.</returns>
        /// <exception cref="ArgumentNullException">The string was null.</exception>
        /// <exception cref="OverflowException">A value obtained from parsing the string exceeded the values allowed for the relevant component.</exception>
        /// <exception cref="FormatException">The string was not in a format that could be parsed to produce an <see cref="OpenGaussTimeSpan"/>.</exception>
        public static OpenGaussTimeSpan Parse(string str)
        {
            if (str == null) {
                throw new ArgumentNullException(nameof(str));
            }
            str = str.Replace('s', ' '); //Quick and easy way to catch plurals.
            try {
                var years = 0;
                var months = 0;
                var days = 0;
                var hours = 0;
                var minutes = 0;
                var seconds = 0m;
                var idx = str.IndexOf("year", StringComparison.Ordinal);
                if (idx > 0) {
                    years = int.Parse(str.Substring(0, idx));
                    str = SafeSubstring(str, idx + 5);
                }
                idx = str.IndexOf("mon", StringComparison.Ordinal);
                if (idx > 0) {
                    months = int.Parse(str.Substring(0, idx));
                    str = SafeSubstring(str, idx + 4);
                }
                idx = str.IndexOf("day", StringComparison.Ordinal);
                if (idx > 0) {
                    days = int.Parse(str.Substring(0, idx));
                    str = SafeSubstring(str, idx + 4).Trim();
                }
                if (str.Length > 0) {
                    var isNegative = str[0] == '-';
                    var parts = str.Split(':');
                    switch (parts.Length) //One of those times that fall-through would actually be good.
                    {
                    case 1:
                        hours = int.Parse(parts[0]);
                        break;
                    case 2:
                        hours = int.Parse(parts[0]);
                        minutes = int.Parse(parts[1]);
                        break;
                    default:
                        hours = int.Parse(parts[0]);
                        minutes = int.Parse(parts[1]);
                        seconds = decimal.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                        break;
                    }
                    if (isNegative) {
                        minutes *= -1;
                        seconds *= -1;
                    }
                }
                var ticks = hours * TicksPerHour + minutes * TicksPerMinute + (long)(seconds * TicksPerSecond);
                return new OpenGaussTimeSpan(years * MonthsPerYear + months, days, ticks);
            } catch (OverflowException) {
                throw;
            } catch (Exception) {
                throw new FormatException();
            }
        }

        private static string SafeSubstring(string s, int startIndex)
        {
            if (startIndex >= s.Length)
                return string.Empty;
            else
                return s.Substring(startIndex);
        }

        /// <summary>
        /// Attempt to parse a <see cref="string"/> to produce an <see cref="OpenGaussTimeSpan"/>.
        /// </summary>
        /// <param name="str">The <see cref="string"/> to parse.</param>
        /// <param name="result">(out) The <see cref="OpenGaussTimeSpan"/> produced, or <see cref="Zero"/> if the parsing failed.</param>
        /// <returns>true if the parsing succeeded, false otherwise.</returns>
        public static bool TryParse(string str, out OpenGaussTimeSpan result)
        {
            try {
                result = Parse(str);
                return true;
            } catch (Exception) {
                result = Zero;
                return false;
            }
        }

        /// <summary>
        /// Create a <see cref="string"/> representation of the <see cref="OpenGaussTimeSpan"/> instance.
        /// The format returned is of the form:
        /// [M mon[s]] [d day[s]] [HH:mm:ss[.f[f[f[f[f[f[f[f[f]]]]]]]]]]
        /// A zero <see cref="OpenGaussTimeSpan"/> is represented as 00:00:00
        /// <remarks>
        /// Ticks are 100ns, Postgress resolution is only to 1&#xb5;s at most. Hence we lose 1 or more decimal
        /// precision in storing values in the database. Despite this, this method will output that extra
        /// digit of precision. It's forward-compatible with any future increases in resolution up to 100ns,
        /// and also makes this ToString() more applicable to any other use-case.
        /// </remarks>
        /// </summary>
        /// <returns>The <see cref="string"/> representation.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            if (Months != 0) {
                sb.Append(Months).Append(Math.Abs(Months) == 1 ? " mon " : " mons ");
            }
            if (Days != 0) {
                if (Months < 0 && Days > 0) {
                    sb.Append('+');
                }
                sb.Append(Days).Append(Math.Abs(Days) == 1 ? " day " : " days ");
            }
            if (Ticks != 0 || sb.Length == 0) {
                if (Ticks < 0) {
                    sb.Append('-');
                } else if (Days < 0 || (Days == 0 && Months < 0)) {
                    sb.Append('+');
                }
                // calculate total seconds and then subtract total whole minutes in seconds to get just the seconds and fractional part
                var seconds = _ticks / (decimal)TicksPerSecond - (_ticks / TicksPerMinute) * 60;
                sb.Append(Math.Abs(Hours).ToString("D2")).Append(':').Append(Math.Abs(Minutes).ToString("D2")).Append(':').Append(Math.Abs(seconds).ToString("0#.######", System.Globalization.CultureInfo.InvariantCulture.NumberFormat));

            }
            if (sb[sb.Length - 1] == ' ') {
                sb.Remove(sb.Length - 1, 1);
            }
            return sb.ToString();
        }

        #endregion

        #region Common Operators

        /// <summary>
        /// Adds two <see cref="OpenGaussTimeSpan"/> together.
        /// </summary>
        /// <param name="x">The first <see cref="OpenGaussTimeSpan"/> to add.</param>
        /// <param name="y">The second <see cref="OpenGaussTimeSpan"/> to add.</param>
        /// <returns>An <see cref="OpenGaussTimeSpan"/> whose values are the sum of the arguments.</returns>
        public static OpenGaussTimeSpan operator +(OpenGaussTimeSpan x, OpenGaussTimeSpan y)
        {
            return x.Add(y);
        }

        /// <summary>
        /// Subtracts one <see cref="OpenGaussTimeSpan"/> from another.
        /// </summary>
        /// <param name="x">The <see cref="OpenGaussTimeSpan"/> to subtract the other from.</param>
        /// <param name="y">The <see cref="OpenGaussTimeSpan"/> to subtract from the other.</param>
        /// <returns>An <see cref="OpenGaussTimeSpan"/> whose values are the difference of the arguments</returns>
        public static OpenGaussTimeSpan operator -(OpenGaussTimeSpan x, OpenGaussTimeSpan y)
        {
            return x.Subtract(y);
        }

        /// <summary>
        /// Returns true if two <see cref="OpenGaussTimeSpan"/> are exactly the same.
        /// </summary>
        /// <param name="x">The first <see cref="OpenGaussTimeSpan"/> to compare.</param>
        /// <param name="y">The second <see cref="OpenGaussTimeSpan"/> to compare.</param>
        /// <returns>true if the two arguments are exactly the same, false otherwise.</returns>
        public static bool operator ==(OpenGaussTimeSpan x, OpenGaussTimeSpan y)
        {
            return x.Equals(y);
        }

        /// <summary>
        /// Returns false if two <see cref="OpenGaussTimeSpan"/> are exactly the same.
        /// </summary>
        /// <param name="x">The first <see cref="OpenGaussTimeSpan"/> to compare.</param>
        /// <param name="y">The second <see cref="OpenGaussTimeSpan"/> to compare.</param>
        /// <returns>false if the two arguments are exactly the same, true otherwise.</returns>
        public static bool operator !=(OpenGaussTimeSpan x, OpenGaussTimeSpan y)
        {
            return !(x == y);
        }

        /// <summary>
        /// Compares two <see cref="OpenGaussTimeSpan"/> instances to see if the first is less than the second
        /// </summary>
        /// <param name="x">The first <see cref="OpenGaussTimeSpan"/> to compare.</param>
        /// <param name="y">The second <see cref="OpenGaussTimeSpan"/> to compare.</param>
        /// <returns>true if the first <see cref="OpenGaussTimeSpan"/> is less than second, false otherwise.</returns>
        public static bool operator <(OpenGaussTimeSpan x, OpenGaussTimeSpan y)
        {
            return x.UnjustifyInterval().Ticks < y.UnjustifyInterval().Ticks;
        }

        /// <summary>
        /// Compares two <see cref="OpenGaussTimeSpan"/> instances to see if the first is less than or equivalent to the second
        /// </summary>
        /// <param name="x">The first <see cref="OpenGaussTimeSpan"/> to compare.</param>
        /// <param name="y">The second <see cref="OpenGaussTimeSpan"/> to compare.</param>
        /// <returns>true if the first <see cref="OpenGaussTimeSpan"/> is less than or equivalent to second, false otherwise.</returns>
        public static bool operator <=(OpenGaussTimeSpan x, OpenGaussTimeSpan y)
        {
            return x.UnjustifyInterval().Ticks <= y.UnjustifyInterval().Ticks;
        }

        /// <summary>
        /// Compares two <see cref="OpenGaussTimeSpan"/> instances to see if the first is greater than the second
        /// </summary>
        /// <param name="x">The first <see cref="OpenGaussTimeSpan"/> to compare.</param>
        /// <param name="y">The second <see cref="OpenGaussTimeSpan"/> to compare.</param>
        /// <returns>true if the first <see cref="OpenGaussTimeSpan"/> is greater than second, false otherwise.</returns>
        public static bool operator >(OpenGaussTimeSpan x, OpenGaussTimeSpan y)
        {
            return !(x <= y);
        }

        /// <summary>
        /// Compares two <see cref="OpenGaussTimeSpan"/> instances to see if the first is greater than or equivalent the second
        /// </summary>
        /// <param name="x">The first <see cref="OpenGaussTimeSpan"/> to compare.</param>
        /// <param name="y">The second <see cref="OpenGaussTimeSpan"/> to compare.</param>
        /// <returns>true if the first <see cref="OpenGaussTimeSpan"/> is greater than or equivalent to the second, false otherwise.</returns>
        public static bool operator >=(OpenGaussTimeSpan x, OpenGaussTimeSpan y)
        {
            return !(x < y);
        }

        /// <summary>
        /// Returns the instance.
        /// </summary>
        public static OpenGaussTimeSpan operator +(OpenGaussTimeSpan x) => Plus(x);

        /// <summary>
        /// Returns the instance.
        /// </summary>
        public static OpenGaussTimeSpan Plus(in OpenGaussTimeSpan x) => x;

        /// <summary>
        /// Negates an <see cref="OpenGaussTimeSpan"/> instance.
        /// </summary>
        /// <param name="x">An <see cref="OpenGaussTimeSpan"/>.</param>
        /// <returns>The negation of the argument.</returns>
        public static OpenGaussTimeSpan operator -(OpenGaussTimeSpan x) => x.Negate();

        #endregion
    }
}
