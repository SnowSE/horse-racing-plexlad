using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Terminal.Gui.Drawing;

const int TIME_STEP = 200; // milliseconds
const int ODDS = 2; // chance 1 out of ... (ex. 1/10)
const int SLOTS = 20; // amount needed to win

using IApplication app = Application.Create();
app.Init();

using Window window = new() { Title = string.Empty, BorderStyle = LineStyle.None };

string[] names = { "Horse 1", "Horse 2", "Horse 3", "Horse 4" };
var bars = new ProgressBar[4];
var barrier = new Barrier(names.Length + 1); // include main thread
var rnd = new Random();
var raceOver = false;

// declare arrays and configure window
for (int i = 0; i < names.Length; i++)
{
    int row = 2 + i * 2;
    window.Add(new Label()
    {
      Text = names[i],
      X = 1,
      Y = row,
      Width = 9,
    });
    bars[i] = new ProgressBar()
    {
      X = 11,
      Y = row,
      Width = Dim.Fill(1),
      Height = 1,
      Fraction = 0f,
    };
    window.Add(bars[i]);
}

var timer = new System.Timers.Timer(TIME_STEP);

// main race logic and code
for (int i = 0; i < names.Length; i++)
{
    int idx = i;
    new Thread(() =>
    {
        while (!raceOver)
        {
            barrier.SignalAndWait();
            if (raceOver) break;

            if (rnd.Next(ODDS) == 0)
            {
                // app.Invoke makes sure that the function runs on main thread
                app.Invoke(() =>
                {
                    bars[idx].Fraction = Math.Min(1f, bars[idx].Fraction + 1f / SLOTS);
                    bars[idx].SetNeedsDraw();
                });
            }

            barrier.SignalAndWait();
        }
    })
    { IsBackground = true }.Start();
}

// main thread
timer.Elapsed += (_, _) =>
{
    if (raceOver) { timer.Stop(); return; }

    barrier.SignalAndWait();
    barrier.SignalAndWait();

    app.Invoke(() =>
    {
        var winners = names.Where((_, i) => bars[i].Fraction >= 1f).ToList();
        if (winners.Any() && !raceOver)
        {
            raceOver = true;
            timer.Stop();
            string msg = winners.Count == 1
                ? $"{winners[0]} wins!"
                : $"Tie: {string.Join(", ", winners)}!";
            MessageBox.Query(app, "Race Over", msg, "OK");
            app.RequestStop();
        }
    });
};

timer.Start();
app.Run(window);

app.Run(window);
