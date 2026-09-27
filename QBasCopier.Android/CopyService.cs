using System.Text.Json;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core;

namespace QBasCopier;

/// <summary>
/// Servicio en primer plano para que la copia, el movimiento y la transferencia
/// sigan con la pantalla apagada.
///
/// Por que hace falta y no basta con WakeLock: desde Android 8 el sistema mata
/// los procesos en segundo plano aunque pidas WakeLock, y una copia larga se
/// quedaba a medias sin avisar. Un servicio en primer plano con notificacion
/// visible es lo unico que el sistema respeta: aparece en la sombra de
/// notificaciones y el usuario ve el progreso, que es justo lo que hacen los
/// copiadores buenos.
/// </summary>
[Service(Exported = false, ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeDataSync)]
public class CopyService : Service
{
    public const string ChannelId = "qbas_progress";
    public const int NotifId = 4711;

    /// <summary>La UI escribe aqui el texto y el progreso; no se pasa por Binder.</summary>
    public static string CurrentText = "";
    public static int CurrentPercent;
    public static string CurrentSub = "";

    /// <summary>Pausa y cancelacion desde la notificacion, sin abrir la app.</summary>
    public static Action? PauseAction;
    public static Action? ResumeAction;
    public static Action? CancelAction;

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        // El comando de pausa o cancelacion llega en el Intent, para hacerlo desde
        // la notificacion sin abrir la ventana.
        switch (intent?.Action)
        {
            case "pause": PauseAction?.Invoke(); break;
            case "resume": ResumeAction?.Invoke(); break;
            case "cancel": CancelAction?.Invoke(); break;
        }
        CreateChannel();
        StartInForeground();
        return StartCommandResult.Sticky;
    }


    private void CreateChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var ch = new NotificationChannel(ChannelId, QBasCopier.MainWindow.AppName, NotificationImportance.Low);
            var mgr = (NotificationManager?)GetSystemService(NotificationService);
            mgr?.CreateNotificationChannel(ch);
        }
    }

    private Notification Build2()
    {
        var b = new Notification.Builder(this, ChannelId)
            .SetContentTitle("QBasWing Shuttle")
            .SetContentText(CurrentText)
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
            .SetOngoing(true)
            .SetOnlyAlertOnce(true);

        if (CurrentPercent is >= 0 and < 100)
            b.SetProgress(100, CurrentPercent, false);
        else
            b.SetProgress(0, 0, true);

        if (!string.IsNullOrWhiteSpace(CurrentSub))
            b.SetContentText(CurrentText + "  ·  " + CurrentSub);

        // Acciones directas desde la sombra de notificaciones.
        var abrir = PackageManager?.GetLaunchIntentForPackage(PackageName ?? "");
        abrir?.AddFlags(ActivityFlags.NewTask);
        var open = PendingIntent.GetActivity(this, 0, abrir,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var acciones = new List<(string Label, PendingIntent Intent)>();
        if (CurrentPercent is > 0 and < 100)
        {
            acciones.Add(("pause", ActionIntent("pause")));
            acciones.Add(("cancel", ActionIntent("cancel")));
        }
        else
        {
            acciones.Add(("resume", ActionIntent("resume")));
        }
        foreach (var (label, pi) in acciones)
            b.AddAction(new Notification.Action.Builder(Icono(), new global::Java.Lang.String(GetTextLabel(label)), pi).Build());

        b.SetContentIntent(open);
        return b.Build();
    }

    private static int Icono() => global::Android.Resource.Drawable.IcDialogInfo;

    /// <summary>Texto de los botones del aviso, en el idioma que tenga la app puesta.</summary>
    private static string GetTextLabel(string k) => k switch
    {
        "pause" => QBasCopier.L.Get("pause"),
        "cancel" => QBasCopier.L.Get("cancel"),
        _ => QBasCopier.L.Get("resume")
    };

    private PendingIntent ActionIntent(string cmd)
    {
        var i = new Intent(this, typeof(CopyService));
        i.SetAction(cmd);
        return PendingIntent.GetService(this, cmd.GetHashCode(), i,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
    }

    private void StartInForeground()
    {
        var n = Build2();
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            StartForeground(NotifId, n, global::Android.Content.PM.ForegroundService.TypeDataSync);
        else
            StartForeground(NotifId, n);
    }

    /// <summary>Actualiza el texto y el progreso. Barato: se puede llamar a cada tick.</summary>
    public static void Update(string text, int percent, string sub = "")
    {
        CurrentText = text;
        CurrentPercent = percent;
        CurrentSub = sub;
        try
        {
            var app = global::Android.App.Application.Context;
            var svc = app.GetSystemService(global::Java.Lang.Class.FromType(typeof(CopyService))) as CopyService;
            if (svc != null) svc.StartInForeground();
        }
        catch { }
    }

    public static void Start()
    {
        try
        {
            var app = global::Android.App.Application.Context;
            var i = new Intent(app, typeof(CopyService));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                app.StartForegroundService(i);
            else
                app.StartService(i);
        }
        catch { }
    }

    public static void Stop()
    {
        try
        {
            var app = global::Android.App.Application.Context;
            app.StopService(new Intent(app, typeof(CopyService)));
        }
        catch { }
    }


}
