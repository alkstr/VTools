﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VTools.Utilities
{
    public static class YTDLP
    {
        public static class Metadata
        {
            public enum Field
            {
                Title,
                Channel,
                ThumbnailURL,
            }

            public static Process Process(
                string ytdlpPath,
                string url,
                IEnumerable<Field> fields)
            {
                var fieldsArg = string.Join(',', fields.Select(field => field switch
                {
                    Field.Title        => "title",
                    Field.Channel      => "channel",
                    Field.ThumbnailURL => "thumbnail",
                    _                  => null,
                }).Where(f => f != null));

                return new()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = ytdlpPath,
                        Arguments = JoinArguments(url, "-O", fieldsArg, "--encoding utf-8"),
                        RedirectStandardOutput = true,
                        RedirectStandardError  = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding  = Encoding.UTF8,
                        CreateNoWindow = true,
                    }
                };
            }
        }

        public static class Download
        {
            public enum Format
            {
                Best,
                BestAudioOnly,
            }

            public enum Subtitles
            {
                None,
                Embedded,
                File,
            }

            public static Process Process(
                string ytdlpPath,
                string ffmpegPath,
                string url,
                Format format,
                Subtitles subtitles,
                string downloadPath)
            {
                var formatArg = format switch
                {
                    Format.Best          => null,
                    Format.BestAudioOnly => "-f bestaudio",
                    _                    => null,
                };

                var subtitlesArg = subtitles switch
                {
                    Subtitles.None     => null,
                    Subtitles.Embedded => "--embed-subs",
                    Subtitles.File     => "--write-subs",
                    _                  => null,
                };

                return new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = ytdlpPath,
                        Arguments = JoinArguments(
                            $"-P \"{downloadPath}\"",
                            formatArg,
                            subtitlesArg,
                            "-o \"%(title)s [%(id)s].%(ext)s\"",
                            $"--ffmpeg-location \"{ffmpegPath}\"",
                            "--extractor-args \"youtube:player_client=mediaconnect\"",
                            "--encoding utf-8",
                            $"\"{url}\""),
                        RedirectStandardOutput = true,
                        RedirectStandardError  = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding  = Encoding.UTF8,
                        CreateNoWindow = true,
                    }
                };
            }
        }

        public static class Update
        {
            public static Process Process(string ytdlpPath) => new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = ytdlpPath,
                    Arguments = "-U --encoding utf-8",
                    CreateNoWindow = false,
                }
            };
        }

        private static string JoinArguments(params string?[] arguments) =>
            string.Join(' ', arguments.Where(arg => arg != null));
    }
}
