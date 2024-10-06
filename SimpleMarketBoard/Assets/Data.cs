using Lumina.Excel.GeneratedSheets;
using Lumina.Excel;
using System.IO;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Style;

namespace SimpleMarketBoard.Assets;

public static partial class Data
{



    // Lumina Sheets

    // Item
    public static readonly ExcelSheet<Item> ItemSheet = Service.Data.GetExcelSheet<Item>()!;
    public static readonly ExcelSheet<World> WorldSheet = Service.Data.GetExcelSheet<World>()!;


    // Fonts

    // Axis
    public static readonly IFontHandle Axis20 = Service.PluginInterface.UiBuilder.FontAtlas.NewGameFontHandle(
        new GameFontStyle
        (
            GameFontFamily.Axis,
            20.0f
        )
    );
    // Noto Sans
    public static readonly IFontHandle NotoSans17 = Service.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(toolkit =>
        {
            toolkit.OnPreBuild(preBuild =>
            {
                preBuild.AddFontFromFile(
                    Path.Combine(Service.PluginInterface.AssemblyLocation.Directory?.FullName!, "Assets", "SourceHanSans-Regular.otf"),
                    new()
                    {
                        SizePx = 17f
                    }
                );
            });
        });

    // ImGui theme

    public static readonly StyleModel defaultTheme = StyleModel.Deserialize("DS1H4sIAAAAAAAACq1XS3PbOAz+LzpnMnpQlORbE2+bQ7uTadLpbm+MzdiqFcsry27TTP97CZIgIcr2etLqIojChzdB8CUS0SS5jC+ih2jyEv0TTTh8/KvfPy+iWTRhsDCPJjG8peWKLVd8mSuuRyXjIlrY5aWVWNtv8WCJrxbMLDjTKlbRJIWFxnI9BYYYrnWAZXq1DVZTvboZGQmr/6nf2q5O2ad/bS2wVwtKy0W0s6bs7Y9v9vu7lfXsBOfE+x8H1Qlhl7OBH7NW+fkS3cvvvf2f2P/6/cW+P+u34gfGab0VD42cj7VrgH47wOd6PW+/XS28UUXJsiQrEES+NZh8KyHxZZlmeVoxJep6WTdzKgklINIiQO1tu9ltKG+JzCVyl5Zdy75qu7nsHDvLLDsQxCcTYcN8txTKs7OseduJJ0msSSvLDITmBsLIZ57/pt3LjsaZYaAZWsUsjKDezPp67zcGRxBHEEcQLyCjdd9Q25Kk4KyIuUX5T431n8baEtKTeTGB8qxicVGlGMw85knpjGdVnFTc5WEk67ptGrHZkgC8TtwHud5diY76iDEBwvjFSM3ezTql+mEAOZVex/+ug+aCxiYWAoTGADFWAqAw0wyxQBjvjmKDkOcIBcIUCYVeL+Vs9UF0KwcoMdVAaAAQXldTq2IfeHa0DkNEWIoFlmKBpVgQ3NWu71tsrPFlgY4AobmBcNVuuMPAZVWa5LxERdA5kgS3PE8LZoMB2z6PqzL2osLSZbrWsW8wBH8hxTUWdSMF7SM5bnQgDBI3epY49tE+P9hPVV4cIgwsxhUdpdmQG9GJvj23uTn+14fWdnBGpY0awyvy9FFu6x/yXVf7E7XAAANhigQDnOYDSOhOgZkFwiBL2hc98k+Yfk+2T4rxB8J0f6ykkhve34g9tOe8MmJebflAyqf1Yzvb0T4ccwxeonquelBIwspYPW5jFCkLZAQmJVlu8SYiXIvDOmYp7SvTdraq14vbTu5r6Q/elId7DMQZNzzqr6dN/0yPYNSIKSCKbpu2f1+v5dZvMOxFQJhwJYcAw7zhKEV2WpYHsJt627cLdXI7Xa6iByfNAcQxZRi9wdwGE5vpHfQcRF1AjMKgMXbW6bt2vTh1tOWHge/rxbI/Vfkj3MfhtHji2PXsb5r/m16hZu34eicbOeslnSRPlFAGXWTaicW0azf3olvIY6q8cbBv/hb7G+V7M/T/mB7jv8KYcVnVawg+7lgRIKf1E3ENdxbuUPQrhcGonYvG4M4DqWDA3UeNmdEkks3zZikidRkzdwgxKsRhXIyPvg0WQZ3TUUDd887gOu8GIs+62Dx6593JoCkTAHs2GN7FyNMixoRSmctRGfMDmusRl+ummX4IL15ZQSPMIOxEpFdH65Q21cZLDENdcRpG36BQqRu8CZcf39T0dHB7GD68KXunWWZCOAyOP+k5/DfNI3YWDoKjbtRBWvIY+xOV6Seh0s2+6l6BjT2nqfZHQOkOKUyPlm45Fatqxmr15y+4vrSZwxAAAA==")!;

}
