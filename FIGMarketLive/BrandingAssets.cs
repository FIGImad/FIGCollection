using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace FIGMarketLive;

internal enum MonitorIconState { Off, Live, Warning }

/// <summary>Embedded assets work from build and publish output without absolute source paths.</summary>
internal sealed class BrandingAssets : IDisposable
{
    private readonly Dictionary<string,Bitmap> images=[];
    private readonly Dictionary<MonitorIconState,Icon> trayIcons=[];
    public Icon ApplicationIcon { get; }
    public BrandingAssets()
    {
        using var stream=Open("FIGMonitor.ico");
        using var original=new Icon(stream);
        ApplicationIcon=(Icon)original.Clone();
    }
    private static Stream Open(string name)=>typeof(BrandingAssets).Assembly.GetManifestResourceStream("FIGMarketLive.Branding."+name)
        ??throw new InvalidDataException($"Missing branding resource: {name}");
    public Bitmap Image(string name,int size=20)
    {
        string key=name+":"+size;
        if(images.TryGetValue(key,out var image))return image;
        using var stream=Open(name);using var source=System.Drawing.Image.FromStream(stream);
        var result=new Bitmap(size,size,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using(var graphics=Graphics.FromImage(result))
        {
            graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;
            graphics.CompositingQuality=CompositingQuality.HighQuality;
            graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;
            graphics.DrawImage(source,new Rectangle(0,0,size,size));
        }
        images.Add(key,result);return result;
    }
    public Icon TrayIcon(MonitorIconState state)
    {
        if(trayIcons.TryGetValue(state,out var icon))return icon;
        string file=state switch{MonitorIconState.Live=>"FIGMonitorOn.png",MonitorIconState.Warning=>"FIGMonitorWarning.png",_=>"FIGMonitorOff.png"};
        nint handle=Image(file,32).GetHicon();
        try{using var temporary=Icon.FromHandle(handle);icon=(Icon)temporary.Clone();}
        finally{DestroyIcon(handle);}
        trayIcons.Add(state,icon);return icon;
    }
    public void Apply(Button button,string file)
    {button.Image=Image(file);button.TextImageRelation=TextImageRelation.ImageBeforeText;button.ImageAlign=ContentAlignment.MiddleLeft;button.Padding=new Padding(4,2,4,2);}
    public static MonitorIconState SelectState(bool monitoring,bool connected,bool needsAttention)=>needsAttention?MonitorIconState.Warning:
        monitoring&&connected?MonitorIconState.Live:MonitorIconState.Off;
    public void Dispose(){foreach(var i in trayIcons.Values)i.Dispose();foreach(var i in images.Values)i.Dispose();ApplicationIcon.Dispose();}
    [DllImport("user32.dll")]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(nint icon);
}
