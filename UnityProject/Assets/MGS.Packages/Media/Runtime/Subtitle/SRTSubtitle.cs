/*************************************************************************
 *  Copyright © 2019 Mogoson. All rights reserved.
 *------------------------------------------------------------------------
 *  File         :  SRTSubtitle.cs
 *  Description  :  SRT subtitle of video.
 *------------------------------------------------------------------------
 *  Author       :  Mogoson
 *  Version      :  0.1.0
 *  Date         :  4/25/2019
 *  Description  :  Initial development version.
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace MGS.Media.Subtitle
{
    /// <summary>
    /// SRT subtitle of video.
    /// </summary>
    public class SRTSubtitle : Subtitle
    {
        #region Field and Property
        /// <summary>
        /// Lines count of a clip.
        /// </summary>
        public const int CLIP_LINES = 3;

        /// <summary>
        /// Separator of new line.
        /// </summary>
        public static readonly string[] NEWLINE_SEPARATOR = new string[] { "\r", "\n", "\r\n" };

        /// <summary>
        /// Separator of clip time range.
        /// </summary>
        public static readonly string[] TIMERANGE_SEPARATOR = new string[] { "-->" };

        /// <summary>
        /// Separator of clip time.
        /// </summary>
        public static readonly string[] TIME_SEPARATOR = new string[] { ":", "," };
        #endregion

        #region Protected Method
        /// <summary>
        /// Parse text to subtitle clip.
        /// </summary>
        /// <param name="index">Index text of clip.</param>
        /// <param name="timeRange">The text of clip time range.</param>
        /// <param name="centent">Content text of clip.</param>
        /// <returns>Subtitle clip.</returns>
        protected ISubtitleClip ParseToClip(string index, string timeRange, string centent)
        {
            if (string.IsNullOrEmpty(index) || string.IsNullOrEmpty(timeRange))
            {
                Debug.LogErrorFormat("Parse text to subtitle clip error: The index or timeRange is null.");
                return null;
            }

            var clipIndex = 0;
            if (!int.TryParse(index, out clipIndex))
            {
                Debug.LogErrorFormat("Parse text to subtitle clip error: The index \"{0}\" can not parse to int.", index);
                return null;
            }

            var startTime = 0;
            var endTime = 0;
            if (!ParseToTimeRange(timeRange, out startTime, out endTime))
            {
                Debug.LogErrorFormat("Parse text to subtitle clip error: The timeRange \"{0}\" can not parse to start and end time.", timeRange);
                return null;
            }

            return new SubtitleClip(clipIndex, startTime, endTime, centent);
        }

        /// <summary>
        /// Parse text to the time range of subtitle clip.
        /// </summary>
        /// <param name="timeRange">The text of clip time range.</param>
        /// <param name="startTime">Start time(Milliseconds) of clip.</param>
        /// <param name="endTime">End time(Milliseconds) of clip.</param>
        /// <returns>Is parsed?</returns>
        protected bool ParseToTimeRange(string timeRange, out int startTime, out int endTime)
        {
            startTime = 0;
            endTime = 0;

            var times = timeRange.Split(TIMERANGE_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
            if (times.Length < 2)
            {
                return false;
            }

            if (ParseToTime(times[0], out startTime))
            {
                return false;
            }

            if (ParseToTime(times[1], out endTime))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Parse text to the time of subtitle clip.
        /// </summary>
        /// <param name="timeText">The text of clip time.</param>
        /// <param name="time">Time(Milliseconds) of clip.</param>
        /// <returns>Is parsed?</returns>
        protected bool ParseToTime(string timeText, out int time)
        {
            time = 0;
            var items = timeText.Split(TIME_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
            if (items.Length < 3)
            {
                return false;
            }

            var hours = 0;
            if (!int.TryParse(items[0], out hours))
            {
                return false;
            }

            var minute = 0;
            if (!int.TryParse(items[1], out minute))
            {
                return false;
            }

            var seconds = 0;
            if (!int.TryParse(items[2], out seconds))
            {
                return false;
            }

            var millisecond = 0;
            if (items.Length > 3)
            {
                int.TryParse(items[3], out millisecond);
            }

            time = ((hours * 60 + minute) * 60 + seconds) * 1000 + millisecond;
            return true;
        }

        /// <summary>
        /// Clear subtitle cache.
        /// </summary>
        protected virtual void ClearCache()
        {
            clips.Clear();
            clip = null;
        }
        #endregion

        #region Public Method
        /// <summary>
        /// Refresh srt subtitle base on the data source.
        /// </summary>
        /// <param name="source">Data source(type is SRTSubtitleSource) to refresh srt subtitle.</param>
        /// <returns>Succeed?</returns>
        public override bool Refresh(object source)
        {
            ClearCache();

            var data = source as SRTSubtitleSource;
            if (data != null)
            {
                if (string.IsNullOrEmpty(data.source))
                {
                    Debug.LogErrorFormat("Refresh srt subtitle error: The source data can not be null or empty.");
                    return false;
                }

                string[] lines = null;
                if (data.type == SRTSubtitleSourceType.File)
                {
                    lines = File.ReadAllLines(data.source, Encoding.Default);
                }
                else
                {
                    lines = data.source.Split(NEWLINE_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
                }
                Refresh(lines);
            }
            else
            {
                Debug.LogErrorFormat("Refresh srt subtitle error: The type of source is not SRTSubtitleSource.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Refresh srt subtitle base on the data source.
        /// </summary>
        /// <param name="source">Data source to refresh srt subtitle.</param>
        /// <returns>Succeed?</returns>
        public bool Refresh(string[] source)
        {
            ClearCache();

            if (source == null || source.Length < CLIP_LINES)
            {
                Debug.LogErrorFormat("Refresh srt subtitle error: The content of source is null or invalid.");
                return false;
            }

            var lines = new List<string>(source);
            while (lines.Count >= CLIP_LINES)
            {
                if (string.IsNullOrEmpty(lines[0]))
                {
                    lines.RemoveAt(0);
                    continue;
                }

                var newClip = ParseToClip(lines[0], lines[1], lines[2]);
                if (newClip == null)
                {
                    lines.RemoveAt(0);
                    continue;
                }
                else
                {
                    clips.Add(newClip);
                    lines.RemoveRange(0, CLIP_LINES);
                }
            }
            return true;
        }
        #endregion
    }

    /// <summary>
    /// Type of srt subtitle source.
    /// </summary>
    public enum SRTSubtitleSourceType
    {
        /// <summary>
        /// SRTSubtitle from file.
        /// </summary>
        File = 0,

        /// <summary>
        /// SRTSubtitle from text content.
        /// </summary>
        Text = 1
    }

    /// <summary>
    /// Source of srt subtitle.
    /// </summary>
    public class SRTSubtitleSource
    {
        /// <summary>
        /// Source of srt subtitle.
        /// </summary>
        public string source;

        /// <summary>
        /// Type of srt subtitle source.
        /// </summary>
        public SRTSubtitleSourceType type;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="source">Source of srt subtitle.</param>
        /// <param name="type">Type of srt subtitle source.</param>
        public SRTSubtitleSource(string source, SRTSubtitleSourceType type = SRTSubtitleSourceType.File)
        {
            this.source = source;
            this.type = type;
        }
    }
}