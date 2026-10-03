namespace DevAncientNaval.Core.Battle;

/// <summary>Fictional Latin-letter names inspired by each fleet's visual culture.</summary>
public static class CultureNamePools
{
    public sealed record Names(IReadOnlyList<string> Captains, IReadOnlyList<string> Towns);
    private static IReadOnlyList<string> Pool(string names) => Array.AsReadOnly(names.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
    public static IReadOnlyList<string> PirateTowns { get; } = Pool("Blackwake,Skull Anchorage,Cutlass Cove,Deadman's Quay,Raven Shoal,Smuggler's Rest,Ironhook Bay,Crow's Nest,Black Lantern,Gallows Reach,Driftwood Den,Stormfang,Kraken Hollow,Ashen Harbor,Mutineer's Haven,Red Knife Cove,Sharktooth,Saltbone Bay,Rumwatch,Shadow Keel,Dagger Reef,Black Gull,Marrowport,Lost Compass");
    private static readonly IReadOnlyDictionary<FleetColor, Names> Cultures = new Dictionary<FleetColor, Names>
    {
        [FleetColor.Blue] = new(Pool("Whitcombe,Fairfax,Hawthorne,Ashford,Drakewell,Blackwood,Harrington,Elmsworth,Beaumont,Redcliffe,Pembroke,Kingsley,Thornton,Whitfield,Stanhope,Everton,Lockwood,Cavendish,Holcroft,Radnor,Westbrook,Marlowe,Trowbridge,Hadleigh"),
            Pool("Port Ashford,Kingshaven,New Whitcombe,Westmere,Pembroke Bay,Elmsport,New Hawthorne,Blackwood Quay,Eastbridge,Redcliffe Harbor,Stanhope Reach,Northwick,Cavendish Point,Whitfield Landing,Fairhaven,Holcroft Bay,Southmere,New Radnor,Lockwood Harbor,Queenswatch,Thornbury,Westbrook Quay,Hadleigh Cove,Trowbridge Port,Drakewell,Beaumont Isle,Marlowe Reach,New Everton,Seabourne,Harrington Bay,Windsor Quay,Briarwick")),
        [FleetColor.Red] = new(Pool("Qorikan,Illari,Pachari,Amaru,Intiray,Kuntur,Suyana,Tupaq,Sachar,Mayu,Urqon,Waman,Nayra,Quillar,Chaskun,Rumari,Wirayu,Apurim,Kallpa,Yupan,Hatun,Miskari,Ayqar,Antari"),
            Pool("Qori Pukara,Inti Marka,Kuntur Tambo,Suyana Pampa,Amaru Wasi,Illari Qocha,Pachari Llaqta,Hatun Rumi,Mayu Tambo,Waman Pukyu,Anta Suyu,Quilla Wasi,Chaska Marka,Rumi Qocha,Tupaq Pampa,Urqu Tambo,Kallpa Wasi,Nayra Marka,Sacha Pukara,Apuri Qocha,Yupan Wasi,Wirayu Tambo,Antari Pampa,Miska Marka,Qhapa Rumi,Sumaq Wasi,Kantu Tambo,Ayqa Pukara,Intiray Cove,Muyu Qocha,Chaskun Pampa,Qori Suyu")),
        [FleetColor.Purple] = new(Pool("Harukaze,Akihiro,Renji,Takemori,Kaede,Masaru,Sora,Hoshin,Ayame,Daichi,Kazunori,Mizuki,Shinobu,Asahi,Kiyora,Seiji,Tsubaki,Yoriharu,Natsuki,Akane,Tomonori,Isamu,Hotaru,Shizuru"),
            Pool("Sakurahama,Aokawa,Tsukihara,Harumori,Kazeshima,Mizuhara,Hoshisato,Yukihama,Kiyomizu,Akitsuru,Shirakawa,Momijisato,Kurohama,Asahikawa,Tsubakihara,Natsumori,Akane Bay,Hotarushima,Shizukawa,Takemura,Renhama,Sorashima,Ayamegawa,Seihara,Kohakumori,Fuyuhama,Hanashiro,Umekawa,Daichisato,Kaedeshima,Torihama,Yorimori")),
        [FleetColor.White] = new(Pool("Adlerstein,Falken,Weissbach,Albrecht,Konrad,Gerhardt,Ulrich,Walther,Heinrich,Erhard,Dietmar,Reinhard,Ottmar,Leopold,Bernward,Siegmund,Gottfried,Hartwig,Wolfram,Eberhard,Roderich,Brunwald,Armin,Theodor"),
            Pool("Adlerhafen,Falkenburg,Weissfurt,Steinheim,Eichenhafen,Nordbruck,Altenfels,Konradsburg,Silberwald,Rabenheim,Hohenquell,Winterhafen,Rosenfurt,Bergwacht,Wolfenbruck,Grunfeld,Marienfels,Thalheim,Ulrichshafen,Waldendorf,Friedental,Reinhardsquell,Eisenburg,Lindenbruck,Kirchenheim,Grauhafen,Sonnenfels,Bernstadt,Ottmarshafen,Hartwigsburg,Arminsfurt,Roderichstal")),
        [FleetColor.Yellow] = new(Pool("Menhet,Sekhem,Nebaset,Taheri,Khemet,Merun,Ankhari,Senef,Rahetep,Nebiru,Hesira,Tameri,Asetra,Khenti,Setem,Merikara,Nefuret,Usera,Renpet,Paneb,Satet,Djeser,Heka,Neferka"),
            Pool("Per Sekhet,Iunu Gate,Menhet Oasis,Ankhar Quay,Nebaset Harbor,Djeser Sands,Per Tameri,Hesira Reach,Rahetep Watch,Golden Nekhen,Merun Delta,Nefuret Quay,Per Khenti,Khemet Wells,Satet Harbor,Nebiru Landing,Renpet Oasis,Per Setem,Usera Gate,Asetra Sands,Senef Port,Merikara Reach,Heka Terrace,Paneb Quay,Neferka Haven,Taheri Delta,Per Ankhari,Lotus Sekhem,Sun Gate Menhet,Khepri Shore,Amber Tameri,Per Rahetep")),
        [FleetColor.Green] = new(Pool("Elderbough,Thornwarden,Mossheart,Oakwhisper,Ashroot,Willowmere,Briarspine,Hazelwake,Elmkeeper,Cedarvoice,Yewwatcher,Fernstride,Rowansong,Hollowbark,Ivyridge,Birchshade,Rootweaver,Aldergrace,Lichenwise,Bramblekin,Dewbranch,Pinewalker,Sapwarden,Greenmantle"),
            Pool("Elderroot Glade,Mossheart Hollow,Whisperwood,Thornwake Grove,Willowrest,Oakreach,Briarhaven,Hazelshade,Fernsong Dell,Ashroot Vale,Rowanbridge,Cedar Hollow,Yewwatch,Ivybough,Elmwarden Glade,Birchlight,Rootweaver Grove,Alderpool,Dewbranch Hollow,Lichenmere,Pinewhisper,Bramblewatch,Sapling Haven,Greenmantle Dell,Hollowbark Reach,Silverleaf Grove,Oldroot Haven,Windbough,Stillfern Glade,Duskmoss,Thistlebark,Glimmerwood"))
    };
    public static Names For(FleetColor color) => Cultures.TryGetValue(color, out var pool) ? pool : throw new ArgumentOutOfRangeException(nameof(color));
    internal static IEnumerable<string> AllCaptains => Cultures.Values.SelectMany(c => c.Captains);
    internal static IEnumerable<string> AllTowns => Cultures.Values.SelectMany(c => c.Towns).Concat(PirateTowns);
}
