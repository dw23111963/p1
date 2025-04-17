using Sstem;
using System.Windows;
using System.Windows.Input;
using System.windows.Controls;

class Myprg
{
    public static TextBox tb;
    [STAThread]
    public static void Main()
    {
        Window win = new Window();
        tb = new TextBox();
        win.Content = tb;
        tb.Focus();
        win.Show();
        Application app = new Application();

        win.MouseDown += WindowOnMouseDown;
        app.Run(win);

    }
    static void WindowOnMouseDown(object sender, MouseButtonEverArgs args)
    {
        double v, t, a;
        Console.WriteLine("Введите v: ");
        v = Convert.ToDouble(Myprg.tb.Text);
        Console.WriteLine("Введите а: ");
        a = Convert.ToDouble(Console.ReadLine());
        Console.WriteLine("Введите t: ");
        t = Convert.ToDouble(Console.ReadLine());


        Bird p = new Bird();
        p.fall += p.f;
        p.fall += p.gg;

        p.start(0, 0, v, a);

        Console.WriteLine("Start");
        Console.WriteLine(p.x);
        Console.WriteLine(p.y);


        p.gde(t);

        Window win = sender as Window;
        Message.Show(p.x.ToString() + " " + p.y.ToString(), "Start");

    }
}

public class Bird
{
    public double x0, y0, vx0, vy0, x, y, tp, g = 9.81, t;
    public void start (double x, double y, double v, double a)
    {
        x0 = x;
        y0 = y;
        a = Math.PI * a / 180.0;
        vx0 = v * Math.Cos(a);
        tp = 2 * vy0 / g;
    }
    public delegate void BirdFly();
    public event BirdFly fall;


    public void gde(double t)
    {
        if (t >= tp)
            t = tp;
        fall();
    }

    public void f()
    {
        string s = (t * vx0).ToString() + " " + (t * vy0 - g * t * t / 2).ToString();
        MessageBox.Show(s);

    }

    public void gg()
    {
        MessageBox.Show("hello");

    }

}

