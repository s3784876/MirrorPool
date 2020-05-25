using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System;
using SubtitlesParser.Classes;
using SubtitlesParser.Classes.Parsers;
using System.Diagnostics;
using ShellProgressBar;

namespace Mirror_Pool
{
  public class EpisodeListener : IClassifier
  {
    #region Constructors

    private EpisodeListener(string srtPath, string episodePath)
    {
      var parser = new SrtParser();
      Subtitles = parser.ParseStream(File.OpenRead(srtPath), Encoding.UTF8).ToArray();

      EpisodePath = episodePath;
    }



    public EpisodeListener(string srtPath, string episodePath, Actor actor) : this(srtPath, episodePath)
    {
      this.Actor = actor;
    }
    #endregion



    #region  public

    public void AddBar(IProgressBar parent)
    {
      var options = new ProgressBarOptions
      {
        DisplayTimeInRealTime = false
      };

      Bar = parent.Spawn(Subtitles.Length,
          EpisodePath.Split('\\').Last(),
          options);
    }

    public void HideBar()
    {
      Bar.Dispose();
    }
    private enum Response
    {
      NO, YES, PLAY_AGAIN, SKIP, SKIP_EPISODE
    }
    public void CheckAll()
    {

      char[] descision = { 'z', 'm', ' ', 'T', 'I' };
      string[] description = { "NO", "YES", "PLAY AGAIN", "SKIP", "SKIP EPISODE" };
      char response = ' ';

      bool epoisodeSkip = false;
      for (int i = 0; i < Subtitles.Length || epoisodeSkip; i++)
      {
        //Check if already tested
        if (!Actor.CanSkip(Subtitles[i], EpisodePath))
        {
          response = ' ';

          //Play audio until a decision is made
          while (response == ' ')
          {
            PlayAudio(EpisodePath, Subtitles[i]);
            response = ConsoleTools.GetDecision(descision, description);
          }

          switch (response)
          {
            //If accepted by user
            case 'm':
              IsMatch(Subtitles[i]);
              break;

            //If regected
            case 'z':
              IsFail(Subtitles[i]);
              break;

            //If they want to come back to it later
            case 'T':
              break;

            case 'I':
              epoisodeSkip = true;
              break;
          }
        }

        //Clears the console and adds to the progressbar tick
        Bar.Tick(Bar.EstimatedDuration);
      }

      Bar.Tick(Bar.MaxTicks - Bar.CurrentTick);
    }
    #endregion


    #region  private

    private readonly Actor Actor;
    private readonly String EpisodePath;
    private readonly SubtitleItem[] Subtitles;
    private ChildProgressBar Bar;

    private static void PlayAudio(string file, SubtitleItem info)
    {
      var start = TimeSpan.FromMilliseconds(info.StartTime).ToString("c");
      var end = TimeSpan.FromMilliseconds(info.EndTime).ToString("c");

      Console.WriteLine(info.ToString());

      var startInfo = new ProcessStartInfo("ffmpeg", $"-hide_banner -loglevel panic -i \"{Path.GetFullPath(file)}\" " + $"-ss {start} " +
      $"-to {end} " +
      "-f pulse " +
      "default"
      );
      startInfo.WindowStyle = ProcessWindowStyle.Hidden;
      startInfo.CreateNoWindow = true;
      startInfo.RedirectStandardOutput = false;

      Process.Start(startInfo);
    }

    private void IsMatch(SubtitleItem info)
    {
      Task.Run(() => Actor.WriteWav(info, EpisodePath));
    }

    private void IsFail(SubtitleItem info)
    {
      Task.Run(() => Actor.AddToSkip(info, EpisodePath));
    }


    #endregion
  }
}