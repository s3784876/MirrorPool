using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Diagnostics;
using SubtitlesParser.Classes;
using ShellProgressBar;

namespace Mirror_Pool
{
  public class Actor : IDisposable
  {
    public Actor(string name)
    {
      Name = name;

      WavPath = $"./output/{name}/LJ-Speech1.1/wavs";
      Directory.CreateDirectory(WavPath);

      string skipPath = $"./output/{name}/skip.csv";

      AlreadyTested = new List<string[]>();

      if (!File.Exists(skipPath))
        File.Create(skipPath);
      else
      {
        var lines = File.ReadAllLines(skipPath);
        foreach (var item in lines)
          AlreadyTested.Add(item.Split(',', 2));
      }

      //Manage dialogue already matched by a different speaker
      string globalSkipPath = "./output/skip.csv";
      if (!File.Exists(globalSkipPath))
        File.Create(globalSkipPath);
      else
      {
        var lines = File.ReadAllLines(globalSkipPath);
        foreach (var item in lines)
          AlreadyTested.Add(item.Split(',', 2));
      }

      SkipWriter = new StreamWriter(skipPath);

      writer = new StreamWriter($"./output/{name}/LJ-Speech1.1/metadata.csv");

      episodes = GetEpisodes();


      Bar = new ProgressBar(episodes.Length, "Episodes completed", new ProgressBarOptions() { DisplayTimeInRealTime = false });
    }

    private EpisodeListener[] GetEpisodes()
    {
      var seasons = Directory.EnumerateDirectories("../");
      var eps = new List<EpisodeListener>();

      foreach (var season in seasons)
      {
        //Get episodes of the season
        var episodes = Directory.EnumerateFiles(season);

        foreach (var item in episodes)
        {
          if (item.EndsWith(".srt"))
          {
            //Catch floating srts without an episode
            //try
            //{
            //Find Matching episode
            var target = item.Substring(item.Length - 9, 5);
            var targetEpisode = episodes.Last(x =>
            {
              return target == x.Substring(x.Length - 9, 5) && x != item;
            });
            eps.Add(new EpisodeListener(item, targetEpisode, this));

            //}
            //catch { }
          }
        }
      }

      return eps.ToArray();
    }

    public readonly List<String[]> AlreadyTested;
    public readonly string WavPath;
    public readonly TextWriter writer;
    public readonly TextWriter SkipWriter;
    public readonly TextWriter GlobalSkipWriter;
    private readonly string Name;
    private readonly EpisodeListener[] episodes;
    private readonly ProgressBar Bar;

    public void WriteWav(SubtitleItem info, string episode)
    {
      string output = WavName(info, EpisodeCode(episode));

      Process.Start("ffmpeg", $"-hide_banner -i \"{Path.GetFullPath(episode)}\" -ss {info.StartTime} -to {info.EndTime} -f wav {Path.Combine(WavPath, output)} ");

      UpdateCSV(info, output, episode);
      AddToSkip(info, episode);
    }

    private static string WavName(SubtitleItem info, string episodeCode)
    {
      return $"{episodeCode}-{info.StartTime}-{info.EndTime}";
    }

    private static string EpisodeCode(string episode)
    {
      return episode.Substring(episode.Length - 9, 5);
    }

    private void UpdateCSV(SubtitleItem info, string output, string episode)
    {
      writer.WriteLine($"{output}|{info.Lines}|{info.Lines}");

      GlobalSkipWriter.WriteLine(SkipFormat(info, episode));
    }

    private static string SkipFormat(SubtitleItem info, string episode)
    {
      return EpisodeCode(episode) + ',' + info.StartTime;
    }

    public void AddToSkip(SubtitleItem info, string episode)
    {
      SkipWriter.WriteLine(SkipFormat(info, episode));
    }

    public bool CanSkip(SubtitleItem info, string episode)
    {
      string epCode = EpisodeCode(episode),
      startTime = info.StartTime.ToString();

      //If that episode and timecode has been tested
      return AlreadyTested.AsParallel().Any(s =>
      {
        return s[0] == epCode && s[1] == startTime;
      });
    }

    public void CheckAll()
    {
      Bar.MaxTicks = episodes.Length;
      foreach (var item in episodes)
      {
        item.AddBar(Bar);
        item.CheckAll();
        item.HideBar();
        Bar.Tick(Bar.EstimatedDuration);
      }
    }

    public void DisposeAsync()
    {
      Task.Run(() => Dispose());
    }

    public void Dispose()
    {
      writer.Flush();
      writer.DisposeAsync();
    }
  }
}