using System.Runtime.CompilerServices;

namespace ChatterBackend;

public static class ExtensionMethods
{
  extension<T>(T target)
  {
    public T CopyFrom(object source) => CopyFrom<T>(target, source, null);
    public T CopyFrom(object source, string[]? ignoreProperties)
    {
      if (target == null) return target;
      ignoreProperties ??= []; //Array.Empty<string>();
      var propsSource = source.GetType().GetProperties().Where(x => x.CanRead && !ignoreProperties.Contains(x.Name));
      var propsTarget = target.GetType().GetProperties().Where(x => x.CanWrite);

      propsTarget
      .Where(prop => propsSource.Any(x => x.Name == prop.Name))
      .ToList()
      .ForEach(prop =>
      {
        var propSource = propsSource.Where(x => x.Name == prop.Name).First();
        prop.SetValue(target, propSource.GetValue(source));
      });
      return target;
    }
  }

  extension(object source)
  {
    public T TransformTo<T>()
    {
      var propsSource = source.GetType().GetProperties().Where(x => x.CanRead);
      var propsTarget = typeof(T).GetConstructors().First().GetParameters();

      object?[] parameterValues = propsTarget
         .Select(prop => propsSource.Any(x => x.Name == prop.Name)
            ? propsSource.Where(x => x.Name == prop.Name).First().GetValue(source)
            : null)
         .ToArray();
      return (T)Activator.CreateInstance(typeof(T), parameterValues)!;
    }
  }

  private static readonly object LockObject = new();
  extension(ControllerBase controller)
  {
    public void Log(object? msg = null, [CallerMemberName] string callerMethod = "")
    {
      lock (LockObject)
      {
        //Note: Color output requires "launchBrowser": false in launchSettings.json
        Console.BackgroundColor = ConsoleColor.Gray;
        Console.ForegroundColor = ConsoleColor.Black;
        Console.Write($"{DateTime.Now:HH:mm:ss.ff}");
        Console.BackgroundColor = ConsoleColor.Black;
        string method = controller.Request.HttpContext.Request.Method;
        Console.ForegroundColor = method == "GET" ? ConsoleColor.Green : method == "DELETE" ? ConsoleColor.Red : ConsoleColor.Cyan;
        Console.Write($" {controller.Request.HttpContext.Request.Method} ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"{controller.Request.HttpContext.Request.Path}");
        if ($"{controller.Request.QueryString}".Length > 1) Console.Write($"{controller.Request.QueryString} ");
        else Console.Write(" ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"{callerMethod} {msg}");
        Console.ResetColor();
      }
    }
  }
  extension(Microsoft.AspNetCore.SignalR.Hub hub)
  {
    public void Log(object? msg = null, [CallerMemberName] string callerMethod = "")
    {
      //Note: Color output requires "launchBrowser": false in launchSettings.json
      Console.BackgroundColor = ConsoleColor.Gray;
      Console.ForegroundColor = ConsoleColor.Black;
      Console.Write($"{DateTime.Now:HH:mm:ss.ff}");
      Console.BackgroundColor = ConsoleColor.Black;
      Console.ForegroundColor = ConsoleColor.Cyan;
      Console.Write($" {hub.Context.ConnectionId} ");
      Console.ForegroundColor = ConsoleColor.White;
      Console.WriteLine($"{hub.GetType().Name}.{callerMethod} {msg}");
      Console.ResetColor();
    }
  }
}
