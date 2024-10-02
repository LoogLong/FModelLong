﻿using System;
using FModel.Framework;
using FModel.Settings;
using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace FModel.ViewModels.ApiEndpoints.Models;

public class GitHubRelease
{
    [J("assets")] public GitHubAsset[] Assets { get; private set; }
}

public class GitHubAsset : ViewModel
{
    [J("name")] public string Name { get; private set; }
    [J("size")] public int Size { get; private set; }
    [J("download_count")] public int DownloadCount { get; private set; }
    [J("browser_download_url")] public string BrowserDownloadUrl { get; private set; }
    [J("created_at")] public DateTime CreatedAt { get; private set; }

    private bool _isLatest;
    public bool IsLatest
    {
        get => _isLatest;
        set => SetProperty(ref _isLatest, value);
    }
}

public class GitHubCommit : ViewModel
{
    private string _sha;
    [J("sha")]
    public string Sha
    {
        get => _sha;
        set
        {
            SetProperty(ref _sha, value);
            RaisePropertyChanged(nameof(IsCurrent));
            RaisePropertyChanged(nameof(ShortSha));
        }
    }

    [J("commit")] public Commit Commit { get; private set; }
    [J("author")] public Author Author { get; private set; }

    private GitHubAsset _asset;
    public GitHubAsset Asset
    {
        get => _asset;
        set
        {
            SetProperty(ref _asset, value);
            RaisePropertyChanged(nameof(IsDownloadable));
        }
    }

    public bool IsCurrent => Sha == UserSettings.Default.CommitHash;
    public string ShortSha => Sha[..7];
    public bool IsDownloadable => Asset != null;
}

public class Commit
{
    [J("author")] public Author Author { get; set; }
    [J("message")] public string Message { get; set; }
}

public class Author
{
    [J("name")] public string Name { get; set; }
    [J("date")] public DateTime Date { get; set; }
    [J("avatar_url")] public string AvatarUrl { get; set; }
    [J("html_url")] public string HtmlUrl { get; set; }
}
