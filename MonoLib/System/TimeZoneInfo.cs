// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class: TimeZoneInfo
**
**
** Purpose: 
** This class is used to represent a Dynamic TimeZone.  It
** has methods for converting a DateTime between TimeZones,
** and for reading TimeZone data from the Windows Registry
**
**
============================================================*/

namespace System
{
    // Author: Arman Ghazanchyan
    // Created date: 09/04/2007
    // Last updated: 09/17/2007

    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using Microsoft.Win32;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Runtime.Versioning;
    using System.Security.Permissions;
    using System.Linq;
    using System.Threading;

    /// <summary>

    /// ''' Represents a time zone and provides access to all system time zones.

    /// ''' </summary>
    [DebuggerDisplay("{_displayName}")]
    public class TimeZoneInfo : IComparer<TimeZoneInfo>
    {
        private string _id;
        private TimeZoneInformation _tzi = new TimeZoneInformation();
        private string _displayName;
        private const string c_timeZonesRegistryHive = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Time Zones";
        private const string c_timeZonesRegistryHivePermissionList = @"HKEY_LOCAL_MACHINE\" + c_timeZonesRegistryHive;
        private Boolean m_supportsDaylightSavingTime
        {
            get
            {
                return _tzi.daylightName != _tzi.standardName;
            }
        }
        private TimeSpan m_baseUtcOffset
        {
            get
            {
                return new TimeSpan(_tzi.standardBias, 0, 0);
            }
        }

        public TimeSpan BaseUtcOffset
        {
            get
            {
                return m_baseUtcOffset;
            }
        }

        //
        // DateTime uses TimeZoneInfo under the hood for IsDaylightSavingTime, IsAmbiguousTime, and GetUtcOffset.
        // These TimeZoneInfo APIs can throw ArgumentException when an Invalid-Time is passed in.  To avoid this
        // unwanted behavior in DateTime public APIs, DateTime internally passes the
        // TimeZoneInfoOptions.NoThrowOnInvalidTime flag to internal TimeZoneInfo APIs.
        //
        // In the future we can consider exposing similar options on the public TimeZoneInfo APIs if there is enough
        // demand for this alternate behavior.
        //
        [Flags]
        internal enum TimeZoneInfoOptions
        {
            None = 1,
            NoThrowOnInvalidTime = 2
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEMTIME
        {
            public ushort wYear;
            public ushort wMonth;
            public ushort wDayOfWeek;
            public ushort wDay;
            public ushort wHour;
            public ushort wMinute;
            public ushort wSecond;
            public ushort wMilliseconds;

            /// <summary>
            ///         ''' Sets the member values of the time structure.
            ///         ''' </summary>
            ///         ''' <param name="info">A byte array that contains the information of a time.</param>
            [DebuggerHidden()]
            public void SetInfo(byte[] info)
            {
                if (info.Length != Marshal.SizeOf(this))
                    throw new ArgumentException("Information size is incorrect", "info");
                this.wYear = BitConverter.ToUInt16(info, 0);
                this.wMonth = BitConverter.ToUInt16(info, 2);
                this.wDayOfWeek = BitConverter.ToUInt16(info, 4);
                this.wDay = BitConverter.ToUInt16(info, 6);
                this.wHour = BitConverter.ToUInt16(info, 8);
                this.wMinute = BitConverter.ToUInt16(info, 10);
                this.wSecond = BitConverter.ToUInt16(info, 12);
                this.wMilliseconds = BitConverter.ToUInt16(info, 14);
            }

            /// <summary>
            ///         ''' Determines whether the specified System.Object 
            ///         ''' is equal to the current System.Object.
            ///         ''' </summary>
            ///         ''' <param name="obj">The System.Object to compare 
            ///         ''' with the current System.Object.</param>
            [DebuggerHidden()]
            public override bool Equals(object obj)
            {
                if (this.GetType() == obj.GetType())
                {
                    SYSTEMTIME objSt = (SYSTEMTIME)obj;
                    if (this.wDay != objSt.wDay || this.wDayOfWeek != objSt.wDayOfWeek || this.wHour != objSt.wHour || this.wMilliseconds != objSt.wMilliseconds || this.wMinute != objSt.wMinute || this.wMonth != objSt.wMonth || this.wSecond != objSt.wSecond || this.wYear != objSt.wYear)
                        return false;
                    else
                        return true;
                }
                return false;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct TimeZoneInformation
        {
            public int bias;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string standardName;
            public SYSTEMTIME standardDate;
            public int standardBias;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string daylightName;
            public SYSTEMTIME daylightDate;
            public int daylightBias;

            /// <summary>
            ///         ''' Sets the member values of bias, standardBias, 
            ///         ''' daylightBias, standardDate, daylightDate of the structure.
            ///         ''' </summary>
            ///         ''' <param name="info">A byte array that contains the 
            ///         ''' information of the Tzi windows registry key.</param>
            [DebuggerHidden()]
            public void SetBytes(byte[] info)
            {
                if (info.Length != 44)
                    throw new ArgumentException("Information size is incorrect", "info");
                this.bias = BitConverter.ToInt32(info, 0);
                this.standardBias = BitConverter.ToInt32(info, 4);
                this.daylightBias = BitConverter.ToInt32(info, 8);
                byte[] helper = new byte[16];
                Array.Copy(info, 12, helper, 0, 16);
                this.standardDate.SetInfo(helper);
                Array.Copy(info, 28, helper, 0, 16);
                this.daylightDate.SetInfo(helper);
            }

            /// <summary>
            ///         ''' Determines whether the specified System.Object 
            ///         ''' is equal to the current System.Object.
            ///         ''' </summary>
            ///         ''' <param name="obj">The System.Object to compare 
            ///         ''' with the current System.Object.</param>
            [DebuggerHidden()]
            public override bool Equals(object obj)
            {
                if (this.GetType() == obj.GetType())
                {
                    TimeZoneInformation objTzi = (TimeZoneInformation)obj;
                    if (this.bias != objTzi.bias || this.daylightBias != objTzi.daylightBias || this.daylightName != objTzi.daylightName || this.standardBias != objTzi.standardBias || this.standardName != objTzi.standardName || !this.daylightDate.Equals(objTzi.daylightDate) || !this.standardDate.Equals(objTzi.standardDate))
                        return false;
                    else
                        return true;
                }
                return false;
            }
        }

        // ---- SECTION:  members for internal support ---------*
        private enum TimeZoneInfoResult
        {
            Success = 0,
            TimeZoneNotFoundException = 1,
            InvalidTimeZoneException = 2,
            SecurityException = 3
        };

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetTimeZoneInformation(ref TimeZoneInformation lpTimeZoneInformation);



        /// <summary>
        ///     ''' Gets the display name of the time zone.
        ///     ''' </summary>
        public string DisplayName
        {
            [DebuggerHidden()]
            get
            {
                this.Refresh();
                return this._displayName;
            }
        }

        /// <summary>
        ///     ''' Gets the daylight saving name of the time zone.
        ///     ''' </summary>
        public string DaylightName
        {
            [DebuggerHidden()]
            get
            {
                this.Refresh();
                if (this.GetDaylightChanges(this.CurrentTime.Year).Delta == TimeSpan.Zero)
                    return this._tzi.standardName;
                else
                    return this._tzi.daylightName;
            }
        }

        /// <summary>
        ///     ''' Gets the standard name of the time zone.
        ///     ''' </summary>
        public string StandardName
        {
            [DebuggerHidden()]
            get
            {
                this.Refresh();
                return this._tzi.standardName;
            }
        }

        /// <summary>
        ///     ''' Gets the current date and time of the time zone.
        ///     ''' </summary>
        public DateTime CurrentTime
        {
            [DebuggerHidden()]
            get
            {
                return new DateTime(DateTime.UtcNow.Ticks + this.CurrentUtcOffset.Ticks, DateTimeKind.Local);
            }
        }

        /// <summary>
        ///     ''' Gets the current UTC (Coordinated Universal Time) offset of the time zone.
        ///     ''' </summary>
        public TimeSpan CurrentUtcOffset
        {
            [DebuggerHidden()]
            get
            {
                if (this.IsDaylightSavingTime())
                    return new TimeSpan(0, -(this._tzi.bias + this._tzi.daylightBias), 0);
                else
                    return new TimeSpan(0, -this._tzi.bias, 0);
            }
        }

        /// <summary>
        ///     ''' Gets or sets the current time zone for this computer system.
        ///     ''' </summary>
        public static TimeZoneInfo CurrentTimeZone
        {
            [DebuggerHidden()]
            get
            {
                return new TimeZoneInfo(TimeZone.CurrentTimeZone.StandardName);
            }
            [DebuggerHidden()]
            set
            {
                value.Refresh();
                if (!TimeZoneInfo.SetTimeZoneInformation(ref value._tzi))
                    // Throw a Win32Exception
                    throw new System.ComponentModel.Win32Exception();
            }
        }

        /// <summary>
        ///     ''' Gets the standard UTC (Coordinated Universal Time) offset of the time zone.
        ///     ''' </summary>
        public TimeSpan StandardUtcOffset
        {
            [DebuggerHidden()]
            get
            {
                this.Refresh();
                return new TimeSpan(0, -this._tzi.bias, 0);
            }
        }

        /// <summary>
        ///     ''' Gets the id of the time zone.
        ///     ''' </summary>
        public string Id
        {
            [DebuggerHidden()]
            get
            {
                this.Refresh();
                return this._id;
            }
        }



        /// <param name="standardName">A time zone standard name.</param>
        [DebuggerHidden()]
        public TimeZoneInfo(string standardName)
        {
            this.SetValues(standardName);
        }

        private TimeZoneInfo(
                String id,
                TimeSpan baseUtcOffset,
                String displayName,
                String standardDisplayName,
                String daylightDisplayName,
                Boolean disableDaylightSavingTime)
        {
            this._id = id;
            this._tzi.daylightName = daylightDisplayName;
            this._tzi.standardName = standardDisplayName;
            this._tzi.bias = baseUtcOffset.Hours;
        }

        [DebuggerHidden()]
        private TimeZoneInfo()
        {
        }



        /// <summary>
        ///     ''' Gets an array of all time zones on the system.
        ///     ''' </summary>
        [DebuggerHidden()]
        public static TimeZoneInfo[] GetTimeZones()
        {
            List<TimeZoneInfo> tzInfos = new List<TimeZoneInfo>();
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Time Zones", false);
            if (key != null)
            {
                foreach (string zoneName in key.GetSubKeyNames())
                {
                    TimeZoneInfo tzi = new TimeZoneInfo();
                    tzi._id = zoneName;
                    tzi.SetValues();
                    tzInfos.Add(tzi);
                }
                TimeZoneInfo.Sort(tzInfos);
            }
            else
                throw new KeyNotFoundException("Cannot find the windows registry key (Time Zone).");
            return tzInfos.ToArray();
        }

        /// <summary>
        ///     ''' Sorts the elements in a list(Of TimeZoneInfo) 
        ///     ''' object based on standard UTC offset or display name.
        ///     ''' </summary>
        ///     ''' <param name="tzInfos">A time zone list to sort.</param>
        [DebuggerHidden()]
        public new static void Sort(List<TimeZoneInfo> tzInfos)
        {
            tzInfos.Sort(new TimeZoneInfo());
        }

        /// <summary>
        ///     ''' Sorts the elements in an entire one-dimensional TimeZoneInfo 
        ///     ''' array based on standard UTC offset or display name.
        ///     ''' </summary>
        ///     ''' <param name="tzInfos">A time zone array to sort.</param>
        [DebuggerHidden()]
        public new static void Sort(TimeZoneInfo[] tzInfos)
        {
            Array.Sort(tzInfos, new TimeZoneInfo());
        }

        /// <summary>
        ///     ''' Gets a TimeZoneInfo.Object from standard name.
        ///     ''' </summary>
        ///     ''' <param name="standardName">A time zone standard name.</param>
        [DebuggerHidden()]
        public static TimeZoneInfo FromStandardName(string standardName)
        {
            return new TimeZoneInfo(standardName);
        }

        /// <summary>
        ///     ''' Gets a TimeZoneInfo.Object from Id.
        ///     ''' </summary>
        ///     ''' <param name="id">A time zone id that corresponds 
        ///     ''' to the windows registry time zone key.</param>
        [DebuggerHidden()]
        public static TimeZoneInfo FromId(string id)
        {
            if (id != null)
            {
                if (id != string.Empty)
                {
                    RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Time Zones", false);
                    if (key != null)
                    {
                        RegistryKey subKey = key.OpenSubKey(id, false);
                        if (subKey != null)
                        {
                            TimeZoneInfo tzi = new TimeZoneInfo();
                            tzi._id = subKey.Name;
                            tzi._displayName = System.Convert.ToString(subKey.GetValue("Display"));
                            tzi._tzi.daylightName = System.Convert.ToString(subKey.GetValue("Dlt"));
                            tzi._tzi.standardName = System.Convert.ToString(subKey.GetValue("Std"));
                            tzi._tzi.SetBytes((byte[])subKey.GetValue("Tzi"));
                            return tzi;
                        }
                    }
                    else
                        throw new KeyNotFoundException("Cannot find the windows registry key (Time Zone).");
                }
                throw new ArgumentException("Unknown time zone.", "id");
            }
            else
                throw new ArgumentNullException("id", "Value cannot be null.");
        }

        /// <summary>
        ///     ''' Returns the daylight saving time for a particular year.
        ///     ''' </summary>
        ///     ''' <param name="year">The year to which the daylight 
        ///     ''' saving time period applies.</param>
        [DebuggerHidden()]
        public System.Globalization.DaylightTime GetDaylightChanges(int year)
        {
            TimeZoneInformation tzi = new TimeZoneInformation();
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Time Zones", false);
            if (key != null)
            {
                RegistryKey subKey = key.OpenSubKey(this._id, false);
                if (subKey != null)
                {
                    RegistryKey subKey1 = subKey.OpenSubKey("Dynamic DST", false);
                    if (subKey1 != null)
                    {
                        if (Array.IndexOf(subKey1.GetValueNames(), System.Convert.ToString(year)) != -1)
                            tzi.SetBytes((byte[])subKey1.GetValue(System.Convert.ToString(year)));
                        else
                        {
                            this.Refresh();
                            tzi = this._tzi;
                        }
                    }
                    else
                    {
                        this.Refresh();
                        tzi = this._tzi;
                    }
                }
                else
                    throw new Exception("Unknown time zone.");
            }
            else
                throw new KeyNotFoundException("Cannot find the windows registry key (Time Zone).");
            DateTime dStart, dEnd;
            dStart = this.GetStartDate(tzi, year);
            dEnd = this.GetEndDate(tzi, year);
            if (dStart != DateTime.MinValue && dEnd != DateTime.MinValue)
                return new DaylightTime(dStart, dEnd, new TimeSpan(0, -this._tzi.daylightBias, 0));
            else
                return new DaylightTime(dStart, dEnd, new TimeSpan(0, 0, 0));
        }

        /// <summary>
        ///     ''' Returns a value indicating whether this time 
        ///     ''' zone is within a daylight saving time period.
        ///     ''' </summary>
        [DebuggerHidden()]
        public bool IsDaylightSavingTime()
        {
            DateTime dUtcNow = DateTime.UtcNow.AddMinutes(-(this._tzi.bias));
            DateTime sUtcNow = DateTime.UtcNow.AddMinutes(-(this._tzi.bias + this._tzi.daylightBias));
            DaylightTime dt;

            if (this._tzi.daylightDate.wMonth <= this._tzi.standardDate.wMonth)
            {
                // Daylight saving time starts and ends in the same year
                dt = this.GetDaylightChanges(dUtcNow.Year);
                if (dt.Delta != TimeSpan.Zero)
                {
                    if (dUtcNow >= dt.Start && sUtcNow < dt.End)
                        return true;
                    else
                        return false;
                }
            }
            else
            {
                // Daylight saving time starts and ends in diferent years
                dt = this.GetDaylightChanges(sUtcNow.Year);
                if (dt.Delta != TimeSpan.Zero)
                {
                    if (dUtcNow < dt.Start && sUtcNow >= dt.End)
                        return false;
                    else
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     ''' Creates and returns a date and time object.
        ///     ''' </summary>
        ///     ''' <param name="wYear">The year of the date.</param>
        ///     ''' <param name="wMonth">The month of the date.</param>
        ///     ''' <param name="wDay">The week day in the month.</param>
        ///     ''' <param name="wDayOfWeek">The day of the week.</param>
        ///     ''' <param name="wHour">The hour of the date.</param>
        ///     ''' <param name="wMinute">The minute of the date.</param>
        ///     ''' <param name="wSecond">The seconds of the date.</param>
        ///     ''' <param name="wMilliseconds">The milliseconds of the date.</param>
        [DebuggerHidden()]
        private DateTime CreateDate(int wYear, int wMonth, int wDay, int wDayOfWeek, int wHour, int wMinute, int wSecond, int wMilliseconds)
        {
            if (wDay < 1 || wDay > 5)
                throw new ArgumentOutOfRangeException("wDat", wDay, "The value is out of acceptable range (1 to 5).");
            if (wDayOfWeek < 0 || wDayOfWeek > 6)
                throw new ArgumentOutOfRangeException("wDayOfWeek", wDayOfWeek, "The value is out of acceptable range (0 to 6).");
            int daysInMonth = DateTime.DaysInMonth(wYear, wMonth);
            int fDayOfWeek = (int)(new DateTime(wYear, wMonth, 1).DayOfWeek);
            int occurre = 1;
            int day = 1;
            if (fDayOfWeek != wDayOfWeek)
            {
                if (wDayOfWeek == 0)
                    day += 7 - fDayOfWeek;
                else if (wDayOfWeek > fDayOfWeek)
                    day += wDayOfWeek - fDayOfWeek;
                else if (wDayOfWeek < fDayOfWeek)
                    day = wDayOfWeek + fDayOfWeek;
            }
            while (occurre < wDay && day <= daysInMonth - 7)
            {
                day += 7;
                occurre += 1;
            }
            return new DateTime(wYear, wMonth, day, wHour, wMinute, wSecond, wMilliseconds, DateTimeKind.Local);
        }

        /// <summary>
        ///     ''' Gets the starting daylight saving date and time for specified thime zone.
        ///     ''' </summary>
        [DebuggerHidden()]
        private DateTime GetStartDate(TimeZoneInformation tzi, int year)
        {
            var withBlock = tzi.daylightDate;
            if (withBlock.wMonth != 0)
            {
                if (withBlock.wYear == 0)
                    return this.CreateDate(year, withBlock.wMonth, withBlock.wDay, withBlock.wDayOfWeek, withBlock.wHour, withBlock.wMinute, withBlock.wSecond, withBlock.wMilliseconds);
                else
                    return new DateTime(withBlock.wYear, withBlock.wMonth, withBlock.wDay, withBlock.wHour, withBlock.wMinute, withBlock.wSecond, withBlock.wMilliseconds, DateTimeKind.Local);
            }
            return DateTime.MinValue;
        }

        /// <summary>
        ///     ''' Gets the end date of the daylight saving time for specified thime zone.
        ///     ''' </summary>
        [DebuggerHidden()]
        private DateTime GetEndDate(TimeZoneInformation tzi, int year)
        {
            var withBlock = tzi.standardDate;
            if (withBlock.wMonth != 0)
            {
                if (withBlock.wYear == 0)
                    return this.CreateDate(year, withBlock.wMonth, withBlock.wDay, withBlock.wDayOfWeek, withBlock.wHour, withBlock.wMinute, withBlock.wSecond, withBlock.wMilliseconds);
                else
                    return new DateTime(withBlock.wYear, withBlock.wMonth, withBlock.wDay, withBlock.wHour, withBlock.wMinute, withBlock.wSecond, withBlock.wMilliseconds, DateTimeKind.Local);
            }
            return DateTime.MinValue;
        }

        /// <summary>
        ///     ''' Refreshes the information of the time zone object.
        ///     ''' </summary>
        [DebuggerHidden()]
        public void Refresh()
        {
            this.SetValues();
        }

        /// <summary>
        ///     ''' Sets the time zone object's information.
        ///     ''' </summary>
        [DebuggerHidden()]
        private new void SetValues()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Time Zones", false);
            if (key != null)
            {
                RegistryKey subKey = key.OpenSubKey(this._id, false);
                if (subKey != null)
                {
                    this._displayName = System.Convert.ToString(subKey.GetValue("Display"));
                    this._tzi.daylightName = System.Convert.ToString(subKey.GetValue("Dlt"));
                    this._tzi.standardName = System.Convert.ToString(subKey.GetValue("Std"));
                    this._tzi.SetBytes((byte[])subKey.GetValue("Tzi"));
                }
                else
                    throw new Exception("Unknown time zone.");
            }
            else
                throw new KeyNotFoundException("Cannot find the windows registry key (Time Zone).");
        }

        /// <summary>
        ///     ''' Sets the time zone object's information.
        ///     ''' </summary>
        ///     ''' <param name="standardName">A time zone standard name.</param>
        [DebuggerHidden()]
        private new void SetValues(string standardName)
        {
            if (standardName != null)
            {
                bool exist = false;
                if (standardName != string.Empty)
                {
                    RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Time Zones", false);
                    if (key != null)
                    {
                        foreach (string zoneName in key.GetSubKeyNames())
                        {
                            RegistryKey subKey = key.OpenSubKey(zoneName, false);
                            if (System.Convert.ToString(subKey.GetValue("Std")) == standardName)
                            {
                                this._id = zoneName;
                                this._displayName = System.Convert.ToString(subKey.GetValue("Display"));
                                this._tzi.daylightName = System.Convert.ToString(subKey.GetValue("Dlt"));
                                this._tzi.standardName = System.Convert.ToString(subKey.GetValue("Std"));
                                this._tzi.SetBytes((byte[])subKey.GetValue("Tzi"));
                                exist = true;
                                break;
                            }
                        }
                    }
                    else
                        throw new KeyNotFoundException("Cannot find the windows registry key (Time Zone).");
                }
                if (!exist)
                    throw new ArgumentException("Unknown time zone.", "standardName");
            }
            else
                throw new ArgumentNullException("id", "Value cannot be null.");
        }

        /// <summary>
        ///     ''' Returns a System.String that represents the current TimeZoneInfo object.
        ///     ''' </summary>
        [DebuggerHidden()]
        public override string ToString()
        {
            return this.DisplayName;
        }

        /// <summary>
        ///     ''' Determines whether the specified System.Object 
        ///     ''' is equal to the current System.Object.
        ///     ''' </summary>
        ///     ''' <param name="obj">The System.Object to compare 
        ///     ''' with the current System.Object.</param>
        [DebuggerHidden()]
        public override bool Equals(object obj)
        {
            if (this.GetType() == obj.GetType())
            {
                TimeZoneInfo objTzi = (TimeZoneInfo)obj;
                if (this._displayName != objTzi._displayName || this._id != objTzi._id || !this._tzi.Equals(objTzi._tzi))
                    return false;
                else
                    return true;
            }
            return false;
        }

        // -------- SECTION: factory methods -----------------*

        //
        // CreateCustomTimeZone -
        // 
        // returns a simple TimeZoneInfo instance that does
        // not support Daylight Saving Time
        //
        static public TimeZoneInfo CreateCustomTimeZone(
                String id,
                TimeSpan baseUtcOffset,
                String displayName,
                  String standardDisplayName)
        {

            return new TimeZoneInfo(
                           id,
                           baseUtcOffset,
                           displayName,
                           standardDisplayName,
                           standardDisplayName,
                           false);
        }

        class CachedData
        {
            private volatile TimeZoneInfo m_localTimeZone;
            private volatile TimeZoneInfo m_utcTimeZone;
            public Dictionary<string, TimeZoneInfo> m_systemTimeZones;
            public bool m_allSystemTimeZonesRead;

            private TimeZoneInfo CreateLocal()
            {
                lock (this)
                {
                    TimeZoneInfo timeZone = m_localTimeZone;
                    if (timeZone == null)
                    {
                        timeZone = TimeZoneInfo.GetLocalTimeZone(this);

                        // this step is to break the reference equality
                        // between TimeZoneInfo.Local and a second time zone
                        // such as "Pacific Standard Time"
                        timeZone = new TimeZoneInfo(
                                            timeZone._id,
                                            timeZone.BaseUtcOffset,
                                            timeZone._displayName,
                                            timeZone.DisplayName,
                                            timeZone.DaylightName,
                                            false);

                        m_localTimeZone = timeZone;
                    }
                    return timeZone;
                }
            }

            public TimeZoneInfo Local
            {
                get
                {
                    TimeZoneInfo timeZone = m_localTimeZone;
                    if (timeZone == null)
                    {
                        timeZone = CreateLocal();
                    }
                    return timeZone;
                }
            }

            public TimeZoneInfo Utc
            {
                get
                {
                    TimeZoneInfo timeZone = m_utcTimeZone;
                    if (timeZone == null)
                    {
                        timeZone = CreateUtc();
                    }
                    return timeZone;
                }
            }

            private TimeZoneInfo CreateUtc()
            {
                lock (this)
                {
                    TimeZoneInfo timeZone = m_utcTimeZone;
                    if (timeZone == null)
                    {
                        timeZone = CreateCustomTimeZone(c_utcId, TimeSpan.Zero, c_utcId, c_utcId);
                        m_utcTimeZone = timeZone;
                    }
                    return timeZone;
                }
            }

            //
            // GetCorrespondingKind-
            //
            // Helper function that returns the corresponding DateTimeKind for this TimeZoneInfo
            //
            public DateTimeKind GetCorrespondingKind(TimeZoneInfo timeZone)
            {
                DateTimeKind kind;

                //
                // we check reference equality to see if 'this' is the same as
                // TimeZoneInfo.Local or TimeZoneInfo.Utc.  This check is needed to 
                // support setting the DateTime Kind property to 'Local' or
                // 'Utc' on the ConverTime(...) return value.  
                //
                // Using reference equality instead of value equality was a 
                // performance based design compromise.  The reference equality
                // has much greater performance, but it reduces the number of
                // returned DateTime's that can be properly set as 'Local' or 'Utc'.
                //
                // For example, the user could be converting to the TimeZoneInfo returned
                // by FindSystemTimeZoneById("Pacific Standard Time") and their local
                // machine may be in Pacific time.  If we used value equality to determine
                // the corresponding Kind then this conversion would be tagged as 'Local';
                // where as we are currently tagging the returned DateTime as 'Unspecified'
                // in this example.  Only when the user passes in TimeZoneInfo.Local or
                // TimeZoneInfo.Utc to the ConvertTime(...) methods will this check succeed.
                //
                if ((object)timeZone == (object)m_utcTimeZone)
                {
                    kind = DateTimeKind.Utc;
                }
                else if ((object)timeZone == (object)m_localTimeZone)
                {
                    kind = DateTimeKind.Local;
                }
                else
                {
                    kind = DateTimeKind.Unspecified;
                }

                return kind;
            }
        }

        //
        // ClearCachedData -
        //
        // Clears data from static members
        //
        static public void ClearCachedData()
        {
            // Clear a fresh instance of cached data
            s_cachedData = new CachedData();
        }

        //
        // ConvertTimeBySystemTimeZoneId -
        //
        // Converts the value of a DateTime object from sourceTimeZone to destinationTimeZone
        //
        static public DateTimeOffset ConvertTimeBySystemTimeZoneId(DateTimeOffset dateTimeOffset, String destinationTimeZoneId)
        {
            return ConvertTime(dateTimeOffset, FindSystemTimeZoneById(destinationTimeZoneId));
        }
        static public DateTime ConvertTimeBySystemTimeZoneId(DateTime dateTime, String destinationTimeZoneId)
        {
            return ConvertTime(dateTime, FindSystemTimeZoneById(destinationTimeZoneId));
        }
        static public DateTime ConvertTimeToUtcBySystemTimeZoneId(DateTime dateTime, String destinationTimeZoneId)
        {
            return ConvertTimeToUtc(dateTime, FindSystemTimeZoneById(destinationTimeZoneId));
        }

        static public DateTime ConvertTimeBySystemTimeZoneId(DateTime dateTime, String sourceTimeZoneId, String destinationTimeZoneId)
        {
            if (dateTime.Kind == DateTimeKind.Local && String.Compare(sourceTimeZoneId, TimeZoneInfo.Local.Id, StringComparison.OrdinalIgnoreCase) == 0)
            {
                // TimeZoneInfo.Local can be cleared by another thread calling TimeZoneInfo.ClearCachedData.
                // Take snapshot of cached data to guarantee this method will not be impacted by the ClearCachedData call.
                // Without the snapshot, there is a chance that ConvertTime will throw since 'source' won't
                // be reference equal to the new TimeZoneInfo.Local
                //
                CachedData cachedData = s_cachedData;
                return ConvertTime(dateTime, cachedData.Local, FindSystemTimeZoneById(destinationTimeZoneId), TimeZoneInfoOptions.None, cachedData);
            }
            else if (dateTime.Kind == DateTimeKind.Utc && String.Compare(sourceTimeZoneId, TimeZoneInfo.Utc.Id, StringComparison.OrdinalIgnoreCase) == 0)
            {
                // TimeZoneInfo.Utc can be cleared by another thread calling TimeZoneInfo.ClearCachedData.
                // Take snapshot of cached data to guarantee this method will not be impacted by the ClearCachedData call.
                // Without the snapshot, there is a chance that ConvertTime will throw since 'source' won't
                // be reference equal to the new TimeZoneInfo.Utc
                //
                CachedData cachedData = s_cachedData;
                return ConvertTime(dateTime, cachedData.Utc, FindSystemTimeZoneById(destinationTimeZoneId), TimeZoneInfoOptions.None, cachedData);
            }
            else
            {
                return ConvertTime(dateTime, FindSystemTimeZoneById(sourceTimeZoneId), FindSystemTimeZoneById(destinationTimeZoneId));
            }
        }
        //
        // ConvertTime -
        //
        // Converts the value of the dateTime object from sourceTimeZone to destinationTimeZone
        //

        static public DateTimeOffset ConvertTime(DateTimeOffset dateTimeOffset, TimeZoneInfo destinationTimeZone)
        {
            if (destinationTimeZone == null)
            {
                throw new ArgumentNullException("destinationTimeZone");
            }

            Contract.EndContractBlock();
            // calculate the destination time zone offset
            DateTime utcDateTime = dateTimeOffset.UtcDateTime;
            TimeSpan destinationOffset = destinationTimeZone.CurrentUtcOffset;

            // check for overflow
            Int64 ticks = utcDateTime.Ticks + destinationOffset.Ticks;

            if (ticks > DateTimeOffset.MaxValue.Ticks)
            {
                return DateTimeOffset.MaxValue;
            }
            else if (ticks < DateTimeOffset.MinValue.Ticks)
            {
                return DateTimeOffset.MinValue;
            }
            else
            {
                return new DateTimeOffset(ticks, destinationOffset);
            }
        }

        static public DateTime ConvertTimeToUtc(DateTime dateTime, TimeZoneInfo destinationTimeZone)
        {
            if (destinationTimeZone == null)
            {
                throw new ArgumentNullException("destinationTimeZone");
            }
            Contract.EndContractBlock();

            // Special case to give a way clearing the cache without exposing ClearCachedData()
            if (dateTime.Ticks == 0)
            {
                ClearCachedData();
            }
            CachedData cachedData = s_cachedData;
            if (dateTime.Kind == DateTimeKind.Utc)
            {
                return ConvertTimeToUtc(dateTime, cachedData.Utc, destinationTimeZone, TimeZoneInfoOptions.None, cachedData);
            }
            else
            {
                return ConvertTimeToUtc(dateTime, cachedData.Local, destinationTimeZone, TimeZoneInfoOptions.None, cachedData);
            }
        }

        static public DateTime ConvertTime(DateTime dateTime, TimeZoneInfo destinationTimeZone)
        {
            if (destinationTimeZone == null)
            {
                throw new ArgumentNullException("destinationTimeZone");
            }
            Contract.EndContractBlock();

            // Special case to give a way clearing the cache without exposing ClearCachedData()
            if (dateTime.Ticks == 0)
            {
                ClearCachedData();
            }
            CachedData cachedData = s_cachedData;
            if (dateTime.Kind == DateTimeKind.Utc)
            {
                return ConvertTime(dateTime, cachedData.Utc, destinationTimeZone, TimeZoneInfoOptions.None, cachedData);
            }
            else
            {
                return ConvertTime(dateTime, cachedData.Local, destinationTimeZone, TimeZoneInfoOptions.None, cachedData);
            }
        }

        static public DateTime ConvertTime(DateTime dateTime, TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone)
        {
            return ConvertTime(dateTime, sourceTimeZone, destinationTimeZone, TimeZoneInfoOptions.None, s_cachedData);
        }


        static internal DateTime ConvertTime(DateTime dateTime, TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone, TimeZoneInfoOptions flags)
        {
            return ConvertTime(dateTime, sourceTimeZone, destinationTimeZone, flags, s_cachedData);
        }

        //
        // GetLocalTimeZone -
        //
        // Helper function for retrieving the local system time zone.
        //
        // returns a new TimeZoneInfo instance
        //
        // may throw COMException, TimeZoneNotFoundException, InvalidTimeZoneException
        //
        // assumes cachedData lock is taken
        //

        [System.Security.SecuritySafeCritical]  // auto-generated
        static private TimeZoneInfo GetLocalTimeZone(CachedData cachedData)
        {
            return new TimeZoneInfo(TimeZone.CurrentTimeZone.StandardName,
                TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now),
                TimeZone.CurrentTimeZone.StandardName,
                TimeZone.CurrentTimeZone.StandardName,
                TimeZone.CurrentTimeZone.DaylightName,
                false);
        }

        static private DateTime ConvertTime(DateTime dateTime, TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone, TimeZoneInfoOptions flags, CachedData cachedData)
        {
            if (sourceTimeZone == null)
            {
                throw new ArgumentNullException("sourceTimeZone");
            }

            if (destinationTimeZone == null)
            {
                throw new ArgumentNullException("destinationTimeZone");
            }
            Contract.EndContractBlock();

            DateTimeKind sourceKind = cachedData.GetCorrespondingKind(sourceTimeZone);
            if (((flags & TimeZoneInfoOptions.NoThrowOnInvalidTime) == 0) && (dateTime.Kind != DateTimeKind.Unspecified) && (dateTime.Kind != sourceKind))
            {
                throw new ArgumentException(EnvironmentEx.GetResourceString("Argument_ConvertMismatch"), "sourceTimeZone");
            }

            //
            // check to see if the DateTime is in an invalid time range.  This check
            // requires the current AdjustmentRule and DaylightTime - which are also
            // needed to calculate 'sourceOffset' in the normal conversion case.
            // By calculating the 'sourceOffset' here we improve the
            // performance for the normal case at the expense of the 'ArgumentException'
            // case and Loss-less Local special cases.
            //
            //AdjustmentRule sourceRule = sourceTimeZone.GetAdjustmentRuleForTime(dateTime);
            TimeSpan sourceOffset = sourceTimeZone.BaseUtcOffset;

            //if (sourceRule != null)
            //{
            //    sourceOffset = sourceOffset + sourceRule.BaseUtcOffsetDelta;
            //    if (sourceRule.HasDaylightSaving)
            //    {
            //        Boolean sourceIsDaylightSavings = false;
            //        DaylightTimeStruct sourceDaylightTime = GetDaylightTime(dateTime.Year, sourceRule);

            //        // 'dateTime' might be in an invalid time range since it is in an AdjustmentRule
            //        // period that supports DST 
            //        if (((flags & TimeZoneInfoOptions.NoThrowOnInvalidTime) == 0) && GetIsInvalidTime(dateTime, sourceRule, sourceDaylightTime))
            //        {
            //            throw new ArgumentException(Environment.GetResourceString("Argument_DateTimeIsInvalid"), "dateTime");
            //        }
            //        sourceIsDaylightSavings = GetIsDaylightSavings(dateTime, sourceRule, sourceDaylightTime, flags);

            //        // adjust the sourceOffset according to the Adjustment Rule / Daylight Saving Rule
            //        sourceOffset += (sourceIsDaylightSavings ? sourceRule.DaylightDelta : TimeSpan.Zero /*FUTURE: sourceRule.StandardDelta*/);
            //    }
            //}

            DateTimeKind targetKind = cachedData.GetCorrespondingKind(destinationTimeZone);

            // handle the special case of Loss-less Local->Local and UTC->UTC)
            if (dateTime.Kind != DateTimeKind.Unspecified && sourceKind != DateTimeKind.Unspecified
                && sourceKind == targetKind)
            {
                return dateTime;
            }

            Int64 utcTicks = dateTime.Ticks - sourceOffset.Ticks;

            // handle the normal case by converting from 'source' to UTC and then to 'target'
            DateTime targetConverted = ConvertUtcToTimeZone(utcTicks, destinationTimeZone);

            if (targetKind == DateTimeKind.Local)
            {
                // Because the ticks conversion between UTC and local is lossy, we need to capture whether the 
                // time is in a repeated hour so that it can be passed to the DateTime constructor.
                return new DateTime(targetConverted.Ticks, DateTimeKind.Local);
            }
            else
            {
                return new DateTime(targetConverted.Ticks, targetKind);
            }
        }

        static private DateTime ConvertTimeToUtc(DateTime dateTime, TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone, TimeZoneInfoOptions flags, CachedData cachedData)
        {
            if (sourceTimeZone == null)
            {
                throw new ArgumentNullException("sourceTimeZone");
            }

            if (destinationTimeZone == null)
            {
                throw new ArgumentNullException("destinationTimeZone");
            }
            Contract.EndContractBlock();

            DateTimeKind sourceKind = cachedData.GetCorrespondingKind(sourceTimeZone);
            if (((flags & TimeZoneInfoOptions.NoThrowOnInvalidTime) == 0) && (dateTime.Kind != DateTimeKind.Unspecified) && (dateTime.Kind != sourceKind))
            {
                throw new ArgumentException(EnvironmentEx.GetResourceString("Argument_ConvertMismatch"), "sourceTimeZone");
            }

            //
            // check to see if the DateTime is in an invalid time range.  This check
            // requires the current AdjustmentRule and DaylightTime - which are also
            // needed to calculate 'sourceOffset' in the normal conversion case.
            // By calculating the 'sourceOffset' here we improve the
            // performance for the normal case at the expense of the 'ArgumentException'
            // case and Loss-less Local special cases.
            //
            //AdjustmentRule sourceRule = sourceTimeZone.GetAdjustmentRuleForTime(dateTime);
            TimeSpan sourceOffset = sourceTimeZone.BaseUtcOffset;

            //if (sourceRule != null)
            //{
            //    sourceOffset = sourceOffset + sourceRule.BaseUtcOffsetDelta;
            //    if (sourceRule.HasDaylightSaving)
            //    {
            //        Boolean sourceIsDaylightSavings = false;
            //        DaylightTimeStruct sourceDaylightTime = GetDaylightTime(dateTime.Year, sourceRule);

            //        // 'dateTime' might be in an invalid time range since it is in an AdjustmentRule
            //        // period that supports DST 
            //        if (((flags & TimeZoneInfoOptions.NoThrowOnInvalidTime) == 0) && GetIsInvalidTime(dateTime, sourceRule, sourceDaylightTime))
            //        {
            //            throw new ArgumentException(Environment.GetResourceString("Argument_DateTimeIsInvalid"), "dateTime");
            //        }
            //        sourceIsDaylightSavings = GetIsDaylightSavings(dateTime, sourceRule, sourceDaylightTime, flags);

            //        // adjust the sourceOffset according to the Adjustment Rule / Daylight Saving Rule
            //        sourceOffset += (sourceIsDaylightSavings ? sourceRule.DaylightDelta : TimeSpan.Zero /*FUTURE: sourceRule.StandardDelta*/);
            //    }
            //}

            DateTimeKind targetKind = cachedData.GetCorrespondingKind(destinationTimeZone);

            // handle the special case of Loss-less Local->Local and UTC->UTC)
            if (dateTime.Kind != DateTimeKind.Unspecified && sourceKind != DateTimeKind.Unspecified
                && sourceKind == targetKind)
            {
                return dateTime;
            }

            Int64 utcTicks = dateTime.Ticks - sourceOffset.Ticks;

            // handle the normal case by converting from 'source' to UTC and then to 'target'
            DateTime targetConverted = ConvertTimeZoneToUtc(utcTicks, destinationTimeZone);

            if (targetKind == DateTimeKind.Local)
            {
                // Because the ticks conversion between UTC and local is lossy, we need to capture whether the 
                // time is in a repeated hour so that it can be passed to the DateTime constructor.
                return new DateTime(targetConverted.Ticks, DateTimeKind.Local);
            }
            else
            {
                return new DateTime(targetConverted.Ticks, targetKind);
            }
        }

        //
        // ConvertUtcToTimeZone -
        //
        // Helper function that converts a dateTime from UTC into the destinationTimeZone
        //
        // * returns DateTime.MaxValue when the converted value is too large
        // * returns DateTime.MinValue when the converted value is too small
        //
        static private DateTime ConvertUtcToTimeZone(Int64 ticks, TimeZoneInfo destinationTimeZone)
        {
            DateTime utcConverted;
            DateTime localConverted;

            // utcConverted is used to calculate the UTC offset in the destinationTimeZone
            if (ticks > DateTime.MaxValue.Ticks)
            {
                utcConverted = DateTime.MaxValue;
            }
            else if (ticks < DateTime.MinValue.Ticks)
            {
                utcConverted = DateTime.MinValue;
            }
            else
            {
                utcConverted = new DateTime(ticks);
            }

            // verify the time is between MinValue and MaxValue in the new time zone
            TimeSpan offset = destinationTimeZone.CurrentUtcOffset;
            ticks += offset.Ticks;

            if (ticks > DateTime.MaxValue.Ticks)
            {
                localConverted = DateTime.MaxValue;
            }
            else if (ticks < DateTime.MinValue.Ticks)
            {
                localConverted = DateTime.MinValue;
            }
            else
            {
                localConverted = new DateTime(ticks);
            }
            return localConverted;
        }

        static private DateTime ConvertTimeZoneToUtc(Int64 ticks, TimeZoneInfo destinationTimeZone)
        {
            DateTime utcConverted;
            DateTime localConverted;

            // utcConverted is used to calculate the UTC offset in the destinationTimeZone
            if (ticks > DateTime.MaxValue.Ticks)
            {
                utcConverted = DateTime.MaxValue;
            }
            else if (ticks < DateTime.MinValue.Ticks)
            {
                utcConverted = DateTime.MinValue;
            }
            else
            {
                utcConverted = new DateTime(ticks);
            }

            // verify the time is between MinValue and MaxValue in the new time zone
            TimeSpan offset = destinationTimeZone.CurrentUtcOffset;
            ticks -= offset.Ticks;

            if (ticks > DateTime.MaxValue.Ticks)
            {
                localConverted = DateTime.MaxValue;
            }
            else if (ticks < DateTime.MinValue.Ticks)
            {
                localConverted = DateTime.MinValue;
            }
            else
            {
                localConverted = new DateTime(ticks);
            }
            return localConverted;
        }


        // constants for TimeZoneInfo.Local and TimeZoneInfo.Utc
        private const string c_utcId = "UTC";
        private const int c_maxKeyLength = 255;

        static CachedData s_cachedData = new CachedData();

        //
        // Utc -
        //
        // returns a TimeZoneInfo instance that represents Universal Coordinated Time (UTC)
        //
        static public TimeZoneInfo Utc
        {
            get
            {
                return s_cachedData.Utc;
            }
        }

        //
        // Local -
        //
        // returns a TimeZoneInfo instance that represents the local time on the machine.
        // Accessing this property may throw InvalidTimeZoneException or COMException
        // if the machine is in an unstable or corrupt state.
        //
        static public TimeZoneInfo Local
        {
            get
            {
                Contract.Ensures(Contract.Result<TimeZoneInfo>() != null);
                return s_cachedData.Local;
            }
        }

        //
        // FindSystemTimeZoneById -
        //
        // Helper function for retrieving a TimeZoneInfo object by <time_zone_name>.
        // This function wraps the logic necessary to keep the private 
        // SystemTimeZones cache in working order
        //
        // This function will either return a valid TimeZoneInfo instance or 
        // it will throw 'InvalidTimeZoneException' / 'TimeZoneNotFoundException'.
        //
        static public TimeZoneInfo FindSystemTimeZoneById(string id)
        {

            // Special case for Utc as it will not exist in the dictionary with the rest
            // of the system time zones.  There is no need to do this check for Local.Id
            // since Local is a real time zone that exists in the dictionary cache
            if (String.Compare(id, c_utcId, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return TimeZoneInfo.Utc;
            }

            if (id == null)
            {
                throw new ArgumentNullException("id");
            }
            else if (id.Length == 0 || id.Length > c_maxKeyLength || id.Contains("\0"))
            {
                throw new TimeZoneNotFoundException(EnvironmentEx.GetResourceString("TimeZoneNotFound_MissingRegistryData", id));
            }

            TimeZoneInfo value;

            TimeZoneInfoResult result;

            CachedData cachedData = s_cachedData;

            lock (cachedData)
            {
                result = TryGetTimeZone(id, false, out value, cachedData);
            }

            if (result == TimeZoneInfoResult.Success)
            {
                return value;
            }
            else if (result == TimeZoneInfoResult.InvalidTimeZoneException)
            {
                throw new InvalidTimeZoneException(EnvironmentEx.GetResourceString("InvalidTimeZone_InvalidRegistryData", id));
            }
            else if (result == TimeZoneInfoResult.SecurityException)
            {
                throw new SecurityException(EnvironmentEx.GetResourceString("Security_CannotReadRegistryData", id));
            }
            else
            {
                throw new TimeZoneNotFoundException(EnvironmentEx.GetResourceString("TimeZoneNotFound_MissingRegistryData", id));
            }
        }

        //
        // TryGetTimeZone -
        //
        // Helper function for retrieving a TimeZoneInfo object by <time_zone_name>.
        //
        // This function may return null.
        //
        // assumes cachedData lock is taken
        //
        static private TimeZoneInfoResult TryGetTimeZone(string id, Boolean dstDisabled, out TimeZoneInfo value, CachedData cachedData)
        {
            TimeZoneInfoResult result = TimeZoneInfoResult.Success;
            TimeZoneInfo match = null;

            // check the cache
            if (cachedData.m_systemTimeZones != null)
            {
                if (cachedData.m_systemTimeZones.TryGetValue(id, out match))
                {
                    if (dstDisabled && match.m_supportsDaylightSavingTime)
                    {
                        // we found a cache hit but we want a time zone without DST and this one has DST data
                        value = CreateCustomTimeZone(match._id, match.m_baseUtcOffset, match._displayName, match.StandardName);
                    }
                    else
                    {
                        value = new TimeZoneInfo(match._id, match.m_baseUtcOffset, match._displayName, match.StandardName,
                                              match.DaylightName, false);
                    }
                    return result;
                }
            }

            // fall back to reading from the local machine 
            // when the cache is not fully populated               
            if (!cachedData.m_allSystemTimeZonesRead)
            {
                var zones = GetTimeZones();
                match = zones.Where(n => n.Id == id).FirstOrDefault();
                if (match == null)
                {
                    value = null;
                    return TimeZoneInfoResult.TimeZoneNotFoundException;
                }
                if (cachedData.m_systemTimeZones == null)
                {
                    cachedData.m_systemTimeZones = new Dictionary<string, TimeZoneInfo>();
                }

                cachedData.m_systemTimeZones.Add(id, match);

                if (dstDisabled && match.m_supportsDaylightSavingTime)
                {
                    // we found a cache hit but we want a time zone without DST and this one has DST data
                    value = CreateCustomTimeZone(match._id, match.m_baseUtcOffset, match._displayName, match.StandardName);
                }
                else
                {
                    value = new TimeZoneInfo(match._id, match.m_baseUtcOffset, match._displayName, match.StandardName,
                                            match.DaylightName, false);
                }

            }
            else
            {
                result = TimeZoneInfoResult.TimeZoneNotFoundException;
                value = null;
            }

            return result;
        }



        [DebuggerHidden()]
        int IComparer<TimeZoneInfo>.Compare(TimeZoneInfo x, TimeZoneInfo y)
        {
            if (x._tzi.bias == y._tzi.bias)
                return x._displayName.CompareTo(y._displayName);
            if (x._tzi.bias > y._tzi.bias)
                return -1;
            if (x._tzi.bias < y._tzi.bias)
                return 1;
            return -1;
        }
    }
} // namespace System
