using System;
using Npgsql;
using Npgsql.TypeHandling;
using NodaTime;
using NpgsqlTypes;
using Npgsql.BackendMessages;
using System.Diagnostics;

namespace Rocket.Surgery.Extensions.Marten.NodaTime
{
    class TimestampHandler : NpgsqlSimpleTypeHandler<Instant>, INpgsqlSimpleTypeHandler<LocalDateTime>, INpgsqlSimpleTypeHandler<DateTime>, INpgsqlSimpleTypeHandler<NpgsqlDateTime>
    {
        /// <summary>
        /// A deprecated compile-time option of PostgreSQL switches to a floating-point representation of some date/time
        /// fields. Npgsql (currently) does not support this mode.
        /// </summary>
        readonly bool _integerFormat;

        /// <summary>
        /// Whether to convert positive and negative infinity values to Instant.{Max,Min}Value when
        /// an Instant is requested
        /// </summary>
        readonly bool _convertInfinityDateTime;

        internal TimestampHandler(bool integerFormat, bool convertInfinityDateTime)
        {
            _integerFormat = integerFormat;
            _convertInfinityDateTime = convertInfinityDateTime;
        }

        #region Read

        public override Instant Read(NpgsqlReadBuffer buf, int len, FieldDescription fieldDescription = null)
        {
            if (_integerFormat)
            {
                var value = buf.ReadInt64();
                if (_convertInfinityDateTime)
                {
                    if (value == long.MaxValue)
                        return Instant.MaxValue;
                    if (value == long.MinValue)
                        return Instant.MinValue;
                }
                return Decode(value);
            }
            else
            {
                var value = buf.ReadDouble();
                if (_convertInfinityDateTime)
                {
                    if (double.IsPositiveInfinity(value))
                        return Instant.MaxValue;
                    if (double.IsNegativeInfinity(value))
                        return Instant.MinValue;
                }
                return Decode(value);
            }
        }

        LocalDateTime INpgsqlSimpleTypeHandler<LocalDateTime>.Read(NpgsqlReadBuffer buf, int len, FieldDescription fieldDescription)
        {
            if (_integerFormat)
            {
                var value = buf.ReadInt64();
                if (value == long.MaxValue || value == long.MinValue)
                    throw new NpgsqlSafeReadException(new NotSupportedException("Infinity values not supported when reading LocalDateTime, read as Instant instead"));
                return Decode(value).InUtc().LocalDateTime;
            }
            else
            {
                var value = buf.ReadDouble();
                if (double.IsPositiveInfinity(value) || double.IsNegativeInfinity(value))
                    throw new NpgsqlSafeReadException(new NotSupportedException("Infinity values not supported when reading LocalDateTime, read as Instant instead"));
                return Decode(value).InUtc().LocalDateTime;
            }
        }

        static readonly Instant Instant2000 = Instant.FromUtc(2000, 1, 1, 0, 0, 0);

        // value is the number of microseconds from 2000-01-01T00:00:00.
        // Unfortunately NodaTime doesn't have Duration.FromMicroseconds(), so we decompose into milliseconds
        // and nanoseconds
        internal static Instant Decode(long value)
            => Instant2000 + Duration.FromMilliseconds(value / 1000) + Duration.FromNanoseconds(value % 1000 * 1000);

        static readonly Instant Instant0 = Instant.FromUtc(1, 1, 1, 0, 0, 0);

        // This is legacy support for PostgreSQL's old floating-point timestamp encoding - finally removed in PG 10 and not used for a long
        // time. Unfortunately CrateDB seems to use this for some reason.
        internal static Instant Decode(double value)
        {
            Debug.Assert(!double.IsPositiveInfinity(value) && !double.IsNegativeInfinity(value));

            if (value >= 0d)
            {
                var date = (int)value / 86400;
                date += 730119; // 730119 = days since era (0001-01-01) for 2000-01-01
                var microsecondOfDay = (long)((value % 86400d) * 1000000d);

                return Instant0 + Duration.FromDays(date) + Duration.FromNanoseconds(microsecondOfDay * 1000);
            }
            else
            {
                value = -value;
                var date = (int)value / 86400;
                var microsecondOfDay = (long)((value % 86400d) * 1000000d);
                if (microsecondOfDay != 0)
                {
                    ++date;
                    microsecondOfDay = 86400000000L - microsecondOfDay;
                }
                date = 730119 - date; // 730119 = days since era (0001-01-01) for 2000-01-01

                return Instant0 + Duration.FromDays(date) + Duration.FromNanoseconds(microsecondOfDay * 1000);
            }
        }

        #endregion Read

        #region Write

        public override int ValidateAndGetLength(Instant value, NpgsqlParameter parameter)
            => 8;

        int INpgsqlSimpleTypeHandler<LocalDateTime>.ValidateAndGetLength(LocalDateTime value, NpgsqlParameter parameter)
            => 8;

        public override void Write(Instant value, NpgsqlWriteBuffer buf, NpgsqlParameter parameter)
        {
            if (_integerFormat)
            {
                if (_convertInfinityDateTime)
                {
                    if (value == Instant.MaxValue)
                    {
                        buf.WriteInt64(long.MaxValue);
                        return;
                    }
                    if (value == Instant.MinValue)
                    {
                        buf.WriteInt64(long.MinValue);
                        return;
                    }
                }
                WriteInteger(value, buf);
            }
            else
            {
                if (_convertInfinityDateTime)
                {
                    if (value == Instant.MaxValue)
                    {
                        buf.WriteDouble(double.PositiveInfinity);
                        return;
                    }
                    if (value == Instant.MinValue)
                    {
                        buf.WriteDouble(double.NegativeInfinity);
                        return;
                    }
                }
                WriteDouble(value, buf);
            }
        }

        void INpgsqlSimpleTypeHandler<LocalDateTime>.Write(LocalDateTime value, NpgsqlWriteBuffer buf, NpgsqlParameter parameter)
        {
            if (_integerFormat)
                WriteInteger(value.InUtc().ToInstant(), buf);
            else
                WriteDouble(value.InUtc().ToInstant(), buf);
        }

        // We need to write the number of microseconds from 2000-01-01T00:00:00.
        internal static void WriteInteger(Instant instant, NpgsqlWriteBuffer buf)
            => buf.WriteInt64((long)(instant - Instant2000).TotalNanoseconds / 1000);

        // This is legacy support for PostgreSQL's old floating-point timestamp encoding - finally removed in PG 10 and not used for a long
        // time. Unfortunately CrateDB seems to use this for some reason.
        internal static void WriteDouble(Instant instant, NpgsqlWriteBuffer buf)
        {
            var localDateTime = instant.InUtc().LocalDateTime;
            var totalDaysSinceEra = Period.Between(default(LocalDateTime), localDateTime, PeriodUnits.Days).Days;
            var secondOfDay = localDateTime.NanosecondOfDay / 1000000000d;

            if (totalDaysSinceEra >= 730119)
            {
                var uSecsDate = (totalDaysSinceEra - 730119) * 86400d;
                buf.WriteDouble(uSecsDate + secondOfDay);
            }
            else
            {
                var uSecsDate = (730119 - totalDaysSinceEra) * 86400d;
                buf.WriteDouble(-(uSecsDate - secondOfDay));
            }
        }

        DateTime INpgsqlSimpleTypeHandler<DateTime>.Read(NpgsqlReadBuffer buf, int len, FieldDescription fieldDescription)
        {

            if (_integerFormat)
            {
                var value = buf.ReadInt64();
                if (value == long.MaxValue || value == long.MinValue)
                    throw new NpgsqlSafeReadException(new NotSupportedException("Infinity values not supported when reading LocalDateTime, read as Instant instead"));
                return Decode(value).ToDateTimeUtc();
            }
            else
            {
                var value = buf.ReadDouble();
                if (double.IsPositiveInfinity(value) || double.IsNegativeInfinity(value))
                    throw new NpgsqlSafeReadException(new NotSupportedException("Infinity values not supported when reading LocalDateTime, read as Instant instead"));
                return Decode(value).ToDateTimeUtc();
            }
        }

        public new int ValidateAndGetLength(DateTime value, NpgsqlParameter parameter)
            => 8;

        public void Write(DateTime value, NpgsqlWriteBuffer buf, NpgsqlParameter parameter)
        {
            if (_integerFormat)
                WriteInteger(Instant.FromDateTimeUtc(value), buf);
            else
                WriteDouble(Instant.FromDateTimeUtc(value), buf);
        }

        NpgsqlDateTime INpgsqlSimpleTypeHandler<NpgsqlDateTime>.Read(NpgsqlReadBuffer buf, int len, FieldDescription fieldDescription)
        {

            if (_integerFormat)
            {
                var value = buf.ReadInt64();
                if (value == long.MaxValue || value == long.MinValue)
                    throw new NpgsqlSafeReadException(new NotSupportedException("Infinity values not supported when reading LocalDateTime, read as Instant instead"));
                return Decode(value).ToDateTimeUtc();
            }
            else
            {
                var value = buf.ReadDouble();
                if (double.IsPositiveInfinity(value) || double.IsNegativeInfinity(value))
                    throw new NpgsqlSafeReadException(new NotSupportedException("Infinity values not supported when reading LocalDateTime, read as Instant instead"));
                return Decode(value).ToDateTimeUtc();
            }
        }

        public new int ValidateAndGetLength(NpgsqlDateTime value, NpgsqlParameter parameter)
            => 8;

        public void Write(NpgsqlDateTime value, NpgsqlWriteBuffer buf, NpgsqlParameter parameter)
        {
            if (_integerFormat)
                WriteInteger(Instant.FromDateTimeUtc(value.ToDateTime()), buf);
            else
                WriteDouble(Instant.FromDateTimeUtc(value.ToDateTime()), buf);
        }

        #endregion Write
    }
}