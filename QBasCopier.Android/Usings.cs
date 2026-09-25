// Alias globales SOLO para el proyecto Android (no afectan al escritorio).
// Con net8.0-android, el SDK inyecta usings de global::Android.*/Java.* que chocan
// con los nombres de Avalonia y System.*; estos alias ganan siempre.
global using Application = Avalonia.Application;
global using Button = Avalonia.Controls.Button;
global using CheckBox = Avalonia.Controls.CheckBox;
global using ComboBox = Avalonia.Controls.ComboBox;
global using Image = Avalonia.Controls.Image;
global using ListBox = Avalonia.Controls.ListBox;
global using Orientation = Avalonia.Layout.Orientation;
global using ProgressBar = Avalonia.Controls.ProgressBar;
global using TextBox = Avalonia.Controls.TextBox;
global using Window = Avalonia.Controls.Window;
global using Bitmap = Avalonia.Media.Imaging.Bitmap;
global using Color = Avalonia.Media.Color;
global using Directory = System.IO.Directory;
global using Environment = System.Environment;
global using File = System.IO.File;
global using FileStream = System.IO.FileStream;
global using Path = System.IO.Path;
global using Stream = System.IO.Stream;
global using Task = System.Threading.Tasks.Task;
global using Thread = System.Threading.Thread;
global using Timer = System.Threading.Timer;
global using Uri = System.Uri;