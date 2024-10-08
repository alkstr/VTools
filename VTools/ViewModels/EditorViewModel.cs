﻿using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using VTools.Models;
using VTools.Utilities;

namespace VTools.ViewModels;

public partial class EditorViewModel : ViewModelBase
{
    public LocalMediaFile Media { get; } = new();
    public Logger Logger { get; } = new();

    public enum EditResult
    {
        Success,
        NoFFmpegError,
        NoFileError,
        AnotherInProgressError,
    }

    public enum DurationResult
    {
        Success,
        NoFFprobeError,
        NoFileError,
        InvalidOutputError,
    }

    public enum MetadataResult
    {
        Success,
        NoFFprobeError,
        NoFileError,
        InvalidOutputError,
    }

    public async Task<EditResult> EditAsync()
    {
        if (!File.Exists(Configuration.FFmpegPath)) { return EditResult.NoFFmpegError; }
        if (!File.Exists(Media.Path)) { return EditResult.NoFileError; }
        if (Monitor.IsEntered(editLock)) { return EditResult.AnotherInProgressError; }

        Monitor.Enter(editLock);
        Logger.Clear();

        var process = FFmpeg.EditProcess(
            Configuration.FFmpegPath,
            Media.Path,
            Media.Cut ? (Media.CutStart.ToString(), Media.CutEnd.ToString()) : (null, null),
            (Media.ChangeWidth ? (uint?)Media.NewWidth : null, Media.ChangeHeight ? (uint?)Media.NewHeight : null),
            Media.EditedFileName,
            Media.Format);
        process.Start();
        process.OutputDataReceived += OnLogReceived;
        process.ErrorDataReceived += OnLogReceived;
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();
        Monitor.Exit(editLock);
        return EditResult.Success;
    }

    public async Task<DurationResult> DurationAsync()
    {
        if (!File.Exists(Configuration.FFprobePath)) { return DurationResult.NoFFprobeError; }
        if (!File.Exists(Media.Path)) { return DurationResult.NoFileError; }

        var process = FFmpeg.DurationProcess(Configuration.FFprobePath, Media.Path);
        process.Start();
        process.BeginErrorReadLine();
        process.ErrorDataReceived += OnLogReceived;

        var output = await process.StandardOutput.ReadToEndAsync();
        var duration = output.Split(':');
        if (duration.Length != 3) { return DurationResult.InvalidOutputError; }
        Media.Duration = new MediaTime()
        {
            Hours = uint.Parse(duration[0]),
            Minutes = uint.Parse(duration[1]),
            Seconds = (uint)float.Parse(duration[2])
        };
        Media.CutStart = new MediaTime();
        Media.CutEnd = Media.Duration;

        await process.WaitForExitAsync();
        return DurationResult.Success;
    }

    public async Task<MetadataResult> MetadataAsync()
    {
        if (!File.Exists(Configuration.FFprobePath)) { return MetadataResult.NoFFprobeError; }
        if (!File.Exists(Media.Path)) { return MetadataResult.NoFileError; }

        var process = FFmpeg.Metadata.Process(
            Configuration.FFprobePath,
            Media.Path,
            [FFmpeg.Metadata.StreamEntry.Width, FFmpeg.Metadata.StreamEntry.Height],
            [FFmpeg.Metadata.FormatEntry.Duration]);
        process.Start();
        process.BeginErrorReadLine();
        process.ErrorDataReceived += OnLogReceived;

        var output = await process.StandardOutput.ReadToEndAsync();
        var metadata = JsonSerializer.Deserialize<FFmpeg.Metadata.Output>(output, serializerOptions);
        if (metadata == null) { return MetadataResult.InvalidOutputError; }

        var duration = metadata.Format.Duration.Split(':');
        Media.Duration = new MediaTime()
        {
            Hours = uint.Parse(duration[0]),
            Minutes = uint.Parse(duration[1]),
            Seconds = (uint)float.Parse(duration[2])
        };

        if (metadata.Streams.Any())
        {
            Media.Width = metadata.Streams.First().Width;
            Media.Height = metadata.Streams.First().Height;
        }
        else
        {
            Media.Width = null;
            Media.Height = null;
        }

        await process.WaitForExitAsync();
        return MetadataResult.Success;
    }

    private readonly object editLock = new();
    private readonly JsonSerializerOptions serializerOptions = new() { PropertyNameCaseInsensitive = true };

    private void OnLogReceived(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Data)) { return; }
        Dispatcher.UIThread.InvokeAsync(() => Logger.AppendLine(e.Data));
    }
}
