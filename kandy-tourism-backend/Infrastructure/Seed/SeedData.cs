namespace KandyTourism.Api.Infrastructure.Seed;

public sealed record SeedCategory(string Slug, string Name, int DisplayOrder);

public sealed record SeedStartPreset(
    string Slug, string Label, string Description, double Latitude, double Longitude,
    bool CoordinatesApproximate, bool IsDefault, int DisplayOrder);

public sealed record SeedImage(string CommonsPath, string Alt);

public sealed record SeedAttraction(
    string Slug,
    string Name,
    string Category,
    string ShortDescription,
    string[] Description,
    string ImagePath,
    string ImageAlt,
    SeedImage[] Gallery,
    string Address,
    string Area,
    double Latitude,
    double Longitude,
    bool CoordinatesApproximate,
    int? EstimatedVisitDurationMinutes,
    string? OpeningHours,
    string? EntranceFee,
    string[] VisitorTips,
    bool Featured,
    string[] Tags);

/// <summary>Builds Wikimedia Commons URLs. Photos are hotlinked from Commons, not hosted by this API.</summary>
public static class CommonsImage
{
    public const string CreditLabel = "Wikimedia Commons";

    public static string Url(string path, int width = 960) =>
        $"https://upload.wikimedia.org/wikipedia/commons/thumb/{path}/{width}px-{FileName(path)}";

    public static string SourceUrl(string path) => $"https://commons.wikimedia.org/wiki/File:{FileName(path)}";

    private static string FileName(string path) => path[(path.LastIndexOf('/') + 1)..];
}

/// <summary>
/// PROTOTYPE CONTENT extracted from the Angular frontend's former mock data
/// (<c>attractions.mock.ts</c>, <c>start-presets.mock.ts</c> and the category list).
/// This is the single source for both the runtime seeder and <c>SqlScripts/SeedKandyAttractions.sql</c>.
/// </summary>
/// <remarks>
/// Coordinates come from Wikipedia/Wikidata; entries flagged <c>CoordinatesApproximate</c> are estimates.
/// Opening hours, entrance fees and visit durations are UNVERIFIED demo estimates and are labelled as
/// such in the UI. They must not be presented as official information.
/// </remarks>
public static class SeedData
{
    public static readonly IReadOnlyList<SeedCategory> Categories =
    [
        new("heritage", "Heritage & Culture", 1),
        new("religious", "Religious Sites", 2),
        new("nature", "Nature & Gardens", 3),
        new("viewpoints", "Viewpoints", 4),
        new("museums", "Museums", 5),
        new("lakes", "Lakes & Scenic Places", 6),
    ];

    public static readonly IReadOnlyList<SeedStartPreset> StartPresets =
    [
        new("kandy-city-centre", "Kandy City Centre", "Clock Tower, in the heart of town", 7.2931, 80.6347, false, true, 1),
        new("kandy-railway-station", "Kandy Railway Station", "Convenient if you arrive by train", 7.2902, 80.6325, true, false, 2),
    ];

    private static class Img
    {
        public const string Tooth = "e/eb/SL_Kandy_asv2020-01_img33_Sacred_Tooth_Temple.jpg";
        public const string Tooth2 = "f/f3/SL_Kandy_asv2020-01_img34_Sacred_Tooth_Temple.jpg";
        public const string Tooth3 = "2/26/Sri_Lanka%2C_Kandy%2C_Temple_of_the_Tooth%2C_Sri_Dalada_Maligawa.jpg";
        public const string Lake = "d/d3/Kandy_Lake_%289%29.jpg";
        public const string LakeMorning = "1/1a/Kandy_lake_in_the_morning.jpg";
        public const string LakeIsland = "4/4d/Sri_Lanka%2C_Island_on_Kandy_lake.jpg";
        public const string Peradeniya = "9/98/Botanical_Garden_of_Peradeniya_03.jpg";
        public const string Peradeniya2 = "f/f4/Sri_Lanka%2C_Peradeniya_Botanical_Gardens.jpg";
        public const string Peradeniya3 = "a/a7/Sri_Lanka%2C_Peradeniya_Botanical_Gardens%2C_Landscaped_Forest.jpg";
        public const string Bahirawakanda = "d/dd/Bahirawakanda.jpg";
        public const string BahirawakandaNight = "e/e0/Bahirawakanda_Vihara_Buddha_Statue.jpg";
        public const string Udawattakele = "8/8e/Giant_lianas_Udawattakele_Forest_Reserve_Kandy.jpg";
        public const string Udawattakele2 = "a/af/Udawattakele_Forest_Southeast_a.jpg";
        public const string TeaMuseum = "1/13/KANDY_TEA_MUSEUM_KANDY_TOWN_SRI_LANKA_JAN_2013_%288583226551%29.jpg";
        public const string ViewPoint = "5/55/SL_Kandy_asv2020-01_img10_Arthurs_Seat_view.jpg";
        public const string ViewPoint2 = "8/82/View_point_of_Kandy.jpg";
        public const string NationalMuseum = "5/54/Kandy_National_Museum.jpg";
        public const string Garrison = "f/f5/British_Garrison_Cemetery.jpg";
        public const string Degaldoruwa = "c/c2/SL_Kandy_asv2020-01_img49_Degaldoruwa_Temple.jpg";
        public const string Embekka = "2/26/Embekka_Devalaya_01.JPG";
        public const string Embekka2 = "6/61/Embekka_Devalaya_Gaja_Sinha.jpg";
        public const string Lankatilaka = "9/9a/Lankathilaka_Vhihara_-_Le_temple.JPG";
        public const string Gadaladeniya = "9/94/Gadaladeniya_Viharaya_02.JPG";
        public const string Hanthana = "5/54/Hanthana_range.JPG";
        public const string StPauls = "8/8c/SL_Kandy_asv2020-01_img29_StPaul_Church.jpg";
    }

    /// <summary>In catalogue order (the "recommended" sort uses featured first, then this order).</summary>
    public static readonly IReadOnlyList<SeedAttraction> Attractions =
    [
        new(
            Slug: "temple-of-the-sacred-tooth-relic",
            Name: "Temple of the Sacred Tooth Relic",
            Category: "religious",
            ShortDescription: "Sri Lanka’s most revered Buddhist shrine, set within the former royal palace complex beside Kandy Lake.",
            Description:
            [
                "Sri Dalada Maligawa, the Temple of the Sacred Tooth Relic, houses the relic of the tooth of the Buddha and has long been tied to the island’s royal heritage. It forms the heart of the Sacred City of Kandy, a UNESCO World Heritage Site.",
                "Visitors walk through carved doorways, painted ceilings and the octagonal Pattirippuwa, joined by pilgrims bringing flowers and offerings. Drumming ceremonies take place at set times during the day, giving the complex a memorable atmosphere.",
                "The wider complex also includes museums and shrines, so allow time to explore beyond the main hall.",
            ],
            ImagePath: Img.Tooth,
            ImageAlt: "The white Temple of the Sacred Tooth Relic with its red-roofed octagonal pavilion and a Buddhist flag",
            Gallery:
            [
                new(Img.Tooth2, "The moat and white walls of the temple complex"),
                new(Img.Tooth3, "Worshippers before a golden Buddha statue inside the temple"),
            ],
            Address: "Sri Dalada Veediya, Kandy",
            Area: "Kandy City",
            Latitude: 7.2936,
            Longitude: 80.6414,
            CoordinatesApproximate: false,
            EstimatedVisitDurationMinutes: 90,
            OpeningHours: "Early morning to evening (demo estimate)",
            EntranceFee: "Ticket required for foreign visitors (demo estimate)",
            VisitorTips:
            [
                "Dress respectfully: shoulders and knees covered.",
                "Shoes are removed before entering the shrine area.",
                "Visit around a drumming ceremony for the liveliest atmosphere.",
            ],
            Featured: true,
            Tags: ["Sri Dalada Maligawa", "UNESCO", "Buddhist", "temple", "relic", "royal palace"]),

        new(
            Slug: "kandy-lake",
            Name: "Kandy Lake",
            Category: "lakes",
            ShortDescription: "The tranquil, man-made lake at the centre of Kandy, ringed by a shaded lakeside walk.",
            Description:
            [
                "Kandy Lake was created in the early nineteenth century during the reign of Sri Vikrama Rajasinha, the last king of Kandy. Its scalloped “clouds wall” parapet and the small island in the middle are among the city’s most recognisable sights.",
                "A walk around the lake takes you past the Temple of the Sacred Tooth Relic, colonial-era buildings and quiet viewpoints over the water. It is especially pleasant early in the morning or late in the afternoon.",
            ],
            ImagePath: Img.Lake,
            ImageAlt: "Kandy Lake with its fountain, the city and green hills behind",
            Gallery:
            [
                new(Img.LakeMorning, "Kandy Lake at dawn with the hills in silhouette"),
                new(Img.LakeIsland, "The small island in the middle of Kandy Lake"),
            ],
            Address: "Kandy Lake Round, Kandy",
            Area: "Kandy City",
            Latitude: 7.2918,
            Longitude: 80.6436,
            CoordinatesApproximate: true,
            EstimatedVisitDurationMinutes: 45,
            OpeningHours: "Lakeside walk is open throughout the day (demo estimate)",
            EntranceFee: "Free to walk around (demo estimate)",
            VisitorTips:
            [
                "Walk clockwise from the temple for the best views of the island.",
                "Early morning is cooler and quieter.",
            ],
            Featured: true,
            Tags: ["lake", "walk", "Kiri Muhuda", "sunset", "city centre"]),

        new(
            Slug: "royal-botanical-gardens-peradeniya",
            Name: "Royal Botanical Gardens, Peradeniya",
            Category: "nature",
            ShortDescription: "Sprawling gardens beside the Mahaweli River, famous for orchids, palm avenues and giant trees.",
            Description:
            [
                "Just outside Kandy, the Royal Botanical Gardens at Peradeniya spread across a bend of the Mahaweli River. Wide lawns, avenues of royal palms and a celebrated orchid house make it one of the most popular green spaces in Sri Lanka.",
                "Look out for the great Javan fig tree, the spice garden and the suspension bridge over the river. The gardens are large, so comfortable shoes and water are recommended.",
            ],
            ImagePath: Img.Peradeniya,
            ImageAlt: "Colourful flower beds and palms along a path at the Royal Botanical Gardens, Peradeniya",
            Gallery:
            [
                new(Img.Peradeniya2, "Wide green lawns and trees in the Peradeniya gardens"),
                new(Img.Peradeniya3, "Tall trees at the edge of a lawn in the Peradeniya gardens"),
            ],
            Address: "Peradeniya, Kandy",
            Area: "Peradeniya",
            Latitude: 7.2711,
            Longitude: 80.5956,
            CoordinatesApproximate: false,
            EstimatedVisitDurationMinutes: 150,
            OpeningHours: "Daytime, roughly morning to late afternoon (demo estimate)",
            EntranceFee: "Ticket required (demo estimate)",
            VisitorTips:
            [
                "Pick up a map at the entrance; the grounds are extensive.",
                "The orchid house is a highlight and gets busy around midday.",
            ],
            Featured: true,
            Tags: ["botanical garden", "orchids", "Mahaweli", "palms", "picnic", "family"]),

        new(
            Slug: "bahirawakanda-buddha-statue",
            Name: "Bahirawakanda Buddha Statue",
            Category: "viewpoints",
            ShortDescription: "A giant white Buddha statue on a hilltop, offering sweeping views across Kandy.",
            Description:
            [
                "The Bahirawakanda Vihara Buddha Statue watches over Kandy from a hill on the western side of the city. The large seated statue is visible from many parts of town and is a popular place to watch the light change over the valley.",
                "A short but steep approach leads to the temple grounds, where terraces look out over the city, the lake and the surrounding hills.",
            ],
            ImagePath: Img.Bahirawakanda,
            ImageAlt: "The serene face of the giant white Bahirawakanda Buddha statue against a blue sky",
            Gallery: [new(Img.BahirawakandaNight, "The Bahirawakanda statue lit up at night")],
            Address: "Bahirawakanda, Kandy",
            Area: "Bahirawakanda",
            Latitude: 7.2963,
            Longitude: 80.6279,
            CoordinatesApproximate: true,
            EstimatedVisitDurationMinutes: 45,
            OpeningHours: "Daytime (demo estimate)",
            EntranceFee: "Small ticket for visitors (demo estimate)",
            VisitorTips:
            [
                "Late afternoon gives softer light for photographs of the city.",
                "Dress respectfully; this is an active temple.",
            ],
            Featured: true,
            Tags: ["Buddha", "statue", "hilltop", "panorama", "temple"]),

        new(
            Slug: "udawattakele-forest-reserve",
            Name: "Udawattakele Forest Reserve",
            Category: "nature",
            ShortDescription: "A historic forest sanctuary on the hill behind the Temple of the Tooth, laced with shaded trails.",
            Description:
            [
                "Udawattakele was once the royal forest of the Kandyan kings and is now a protected reserve right on the edge of the city. Giant lianas, tall trees and birdsong make it feel far removed from the busy streets below.",
                "Several marked trails loop through the forest, passing a small pond and forest hermitages. It is a rewarding stop for walkers and birdwatchers.",
            ],
            ImagePath: Img.Udawattakele,
            ImageAlt: "Giant twisting lianas among tall trees in Udawattakele Forest Reserve",
            Gallery: [new(Img.Udawattakele2, "Tall, slender trees inside Udawattakele forest")],
            Address: "Udawattakele, Kandy",
            Area: "Kandy City (north)",
            Latitude: 7.2994,
            Longitude: 80.6389,
            CoordinatesApproximate: false,
            EstimatedVisitDurationMinutes: 120,
            OpeningHours: "Daytime (demo estimate)",
            EntranceFee: "Ticket required (demo estimate)",
            VisitorTips:
            [
                "Stay on the marked trails and carry water.",
                "Watch out for monkeys near the entrance with food.",
            ],
            Featured: false,
            Tags: ["forest", "hiking", "birdwatching", "nature trail", "royal forest"]),

        new(
            Slug: "ceylon-tea-museum",
            Name: "Ceylon Tea Museum",
            Category: "museums",
            ShortDescription: "A former tea factory in the Hantana hills telling the story of Ceylon tea, with a tasting room upstairs.",
            Description:
            [
                "The Ceylon Tea Museum occupies the old Hantana tea factory in the hills south of Kandy. Restored machinery and displays explain how tea is withered, rolled, dried and graded, and introduce the pioneers of the island’s tea industry.",
                "The top floor has a tea room with views over the hills, making it a relaxing stop between sightseeing in the city and the countryside.",
            ],
            ImagePath: Img.TeaMuseum,
            ImageAlt: "The multi-storey former factory building that houses the Ceylon Tea Museum",
            Gallery: [],
            Address: "Hantana Road, Kandy",
            Area: "Hantana",
            Latitude: 7.2686,
            Longitude: 80.6327,
            CoordinatesApproximate: false,
            EstimatedVisitDurationMinutes: 75,
            OpeningHours: "Daytime, may close on certain days (demo estimate)",
            EntranceFee: "Ticket required (demo estimate)",
            VisitorTips: ["Combine with a visit to the Hantana hills nearby."],
            Featured: true,
            Tags: ["tea", "museum", "Hantana", "history", "tasting"]),

        new(
            Slug: "kandy-view-point",
            Name: "Kandy View Point",
            Category: "viewpoints",
            ShortDescription: "A classic lookout above the lake with a postcard view of the city and the Temple of the Tooth.",
            Description:
            [
                "Along Rajapihilla Mawatha, above the southern shore of Kandy Lake, this lookout (often linked with the name Arthur’s Seat) frames the lake, the temple and the city spread across the valley.",
                "It is a quick stop by tuk-tuk or a pleasant uphill walk from the lake, and a favourite spot for photographs.",
            ],
            ImagePath: Img.ViewPoint,
            ImageAlt: "Panoramic view over Kandy Lake and the city from the hillside lookout",
            Gallery: [new(Img.ViewPoint2, "Kandy city seen from the view point")],
            Address: "Rajapihilla Mawatha, Kandy",
            Area: "Kandy City (south)",
            Latitude: 7.2886,
            Longitude: 80.6406,
            CoordinatesApproximate: true,
            EstimatedVisitDurationMinutes: 30,
            OpeningHours: null,
            EntranceFee: "Free roadside lookout (demo estimate)",
            VisitorTips: ["Morning light is best for photographing the temple across the lake."],
            Featured: true,
            Tags: ["view point", "Arthur’s Seat", "panorama", "photography", "lake view"]),

        new(
            Slug: "national-museum-of-kandy",
            Name: "National Museum of Kandy",
            Category: "museums",
            ShortDescription: "Kandyan-era treasures displayed in a former royal residence next to the Temple of the Tooth.",
            Description:
            [
                "The National Museum of Kandy is housed in the Palle Vahale, a building associated with the queens of the Kandyan court. Its collection covers weapons, jewellery, tools, ceremonial objects and documents from the Kandyan period.",
                "Because it sits right beside the Temple of the Sacred Tooth Relic, it is easy to combine with a temple visit.",
            ],
            ImagePath: Img.NationalMuseum,
            ImageAlt: "The tiled-roof building of the National Museum of Kandy behind its front railings",
            Gallery: [],
            Address: "Beside the Temple of the Sacred Tooth Relic, Kandy",
            Area: "Kandy City",
            Latitude: 7.2947,
            Longitude: 80.6408,
            CoordinatesApproximate: false,
            EstimatedVisitDurationMinutes: 60,
            OpeningHours: "Daytime, closed on some days (demo estimate)",
            EntranceFee: "Ticket required (demo estimate)",
            VisitorTips: ["Photography rules vary by gallery; check signs inside."],
            Featured: false,
            Tags: ["museum", "Kandyan kingdom", "history", "artefacts", "royal"]),

        new(
            Slug: "kandy-garrison-cemetery",
            Name: "Kandy Garrison Cemetery",
            Category: "heritage",
            ShortDescription: "A small, peaceful British-era cemetery tucked behind the temple, with stories told by its caretaker.",
            Description:
            [
                "The British Garrison Cemetery was used for burials of British residents and soldiers during the colonial period. Its weathered headstones record lives cut short by disease, accidents and the challenges of nineteenth-century Ceylon.",
                "The garden setting is quiet and well kept, and it offers a different perspective on Kandy’s layered history.",
            ],
            ImagePath: Img.Garrison,
            ImageAlt: "Old headstones among lawns and trees at the British Garrison Cemetery in Kandy",
            Gallery: [],
            Address: "Behind the Temple of the Sacred Tooth Relic, Kandy",
            Area: "Kandy City",
            Latitude: 7.2928,
            Longitude: 80.6432,
            CoordinatesApproximate: false,
            EstimatedVisitDurationMinutes: 30,
            OpeningHours: "Daytime (demo estimate)",
            EntranceFee: "Donations welcome (demo estimate)",
            VisitorTips: ["A short uphill walk from the temple entrance."],
            Featured: false,
            Tags: ["colonial", "British", "cemetery", "history"]),

        new(
            Slug: "degaldoruwa-raja-maha-vihara",
            Name: "Degaldoruwa Raja Maha Vihara",
            Category: "religious",
            ShortDescription: "An eighteenth-century rock cave temple renowned for its vivid Kandyan-period murals.",
            Description:
            [
                "Degaldoruwa Raja Maha Vihara is a cave temple carved into a rock outcrop across the Mahaweli River from Kandy. It dates from the reign of King Kirti Sri Rajasinha in the eighteenth century.",
                "The cave walls and ceilings are covered in some of the finest Kandyan-period paintings, illustrating Jataka stories about the previous lives of the Buddha.",
            ],
            ImagePath: Img.Degaldoruwa,
            ImageAlt: "The white entrance buildings and belfry of the Degaldoruwa temple",
            Gallery: [],
            Address: "Degaldoruwa, near Lewella, Kandy",
            Area: "Lewella",
            Latitude: 7.3044,
            Longitude: 80.6639,
            CoordinatesApproximate: false,
            EstimatedVisitDurationMinutes: 60,
            OpeningHours: "Daytime (demo estimate)",
            EntranceFee: "Ticket or donation (demo estimate)",
            VisitorTips: ["Ask before taking photographs of the murals."],
            Featured: false,
            Tags: ["cave temple", "murals", "Jataka", "Kandyan art", "Buddhist"]),

        new(
            Slug: "embekka-devalaya",
            Name: "Embekka Devalaya",
            Category: "heritage",
            ShortDescription: "A medieval shrine famed for its intricately carved wooden pillars and timber craftsmanship.",
            Description:
            [
                "Embekka Devalaya, in the Udunuwara area west of Kandy, is celebrated for the carved wooden pillars of its drummers’ hall. The carvings show dancers, wrestlers, mythical creatures and intricate floral patterns.",
                "It is often visited together with the nearby Lankatilaka and Gadaladeniya temples as part of a countryside heritage loop.",
            ],
            ImagePath: Img.Embekka,
            ImageAlt: "Carved wooden pillars and roof of the Embekka Devalaya hall",
            Gallery: [new(Img.Embekka2, "Detailed wood carving of a mythical creature at Embekka")],
            Address: "Embekka, Udunuwara",
            Area: "Udunuwara",
            Latitude: 7.2179,
            Longitude: 80.5677,
            CoordinatesApproximate: false,
            EstimatedVisitDurationMinutes: 60,
            OpeningHours: "Daytime (demo estimate)",
            EntranceFee: "Ticket required (demo estimate)",
            VisitorTips: ["Pair with Lankatilaka and Gadaladeniya for a half-day loop."],
            Featured: false,
            Tags: ["wood carving", "devalaya", "medieval", "craftsmanship", "Udunuwara"]),

        new(
            Slug: "lankatilaka-vihara",
            Name: "Lankatilaka Vihara",
            Category: "religious",
            ShortDescription: "A fourteenth-century temple built on a rock outcrop, with fine views over the Udunuwara countryside.",
            Description:
            [
                "Lankatilaka Vihara stands on a granite outcrop in the Udunuwara countryside. Built in the fourteenth century during the Gampola period, it blends Sinhalese and South Indian architectural influences.",
                "Inside are an image house and murals, while the rock platform offers peaceful views over paddy fields and hills.",
            ],
            ImagePath: Img.Lankatilaka,
            ImageAlt: "The white, multi-tiered Lankatilaka temple building",
            Gallery: [],
            Address: "Hiyarapitiya, Udunuwara",
            Area: "Udunuwara",
            Latitude: 7.2339,
            Longitude: 80.5647,
            CoordinatesApproximate: false,
            EstimatedVisitDurationMinutes: 60,
            OpeningHours: "Daytime (demo estimate)",
            EntranceFee: "Ticket or donation (demo estimate)",
            VisitorTips: ["There are steps up the rock; wear sensible footwear."],
            Featured: false,
            Tags: ["Gampola period", "rock temple", "countryside", "Buddhist", "views"]),

        new(
            Slug: "gadaladeniya-vihara",
            Name: "Gadaladeniya Vihara",
            Category: "religious",
            ShortDescription: "A stone temple from the fourteenth century showing South Indian architectural influences.",
            Description:
            [
                "Gadaladeniya Vihara is built on a flat rock near Pilimathalawa. Constructed largely of stone in the fourteenth century, its design reflects influences from South Indian temple architecture.",
                "The quiet setting and rock-cut details make it a rewarding stop on the way to Embekka and Lankatilaka.",
            ],
            ImagePath: Img.Gadaladeniya,
            ImageAlt: "The stone shrine of Gadaladeniya Vihara on a rock platform",
            Gallery: [],
            Address: "Gadaladeniya, near Pilimathalawa",
            Area: "Udunuwara",
            Latitude: 7.2573,
            Longitude: 80.5561,
            CoordinatesApproximate: false,
            EstimatedVisitDurationMinutes: 45,
            OpeningHours: "Daytime (demo estimate)",
            EntranceFee: "Ticket or donation (demo estimate)",
            VisitorTips: ["The rock surface can be hot at midday; you may need to remove shoes."],
            Featured: false,
            Tags: ["stone temple", "Gampola period", "architecture", "Buddhist"]),

        new(
            Slug: "hanthana-mountain-range",
            Name: "Hanthana Mountain Range",
            Category: "nature",
            ShortDescription: "Misty ridges and tea-covered slopes just south of Kandy, popular for hiking and cool-weather views.",
            Description:
            [
                "The Hanthana range rises to the south-west of Kandy, with several peaks, tea estates and grassland ridges. It is a favourite with hikers and offers wide views back over the city and towards the central highlands.",
                "Trails vary in difficulty and conditions can change quickly, so it is best tackled early in the day with a local guide.",
            ],
            ImagePath: Img.Hanthana,
            ImageAlt: "A grassy green ridge of the Hanthana mountain range",
            Gallery: [],
            Address: "Hantana, Kandy",
            Area: "Hantana",
            Latitude: 7.2586,
            Longitude: 80.6286,
            CoordinatesApproximate: true,
            EstimatedVisitDurationMinutes: 180,
            OpeningHours: null,
            EntranceFee: null,
            VisitorTips:
            [
                "Start early to avoid afternoon mist and rain.",
                "Consider a local guide; trails are not always signposted.",
            ],
            Featured: false,
            Tags: ["hiking", "tea estates", "mountains", "trekking", "views"]),

        new(
            Slug: "st-pauls-church-kandy",
            Name: "St. Paul’s Church, Kandy",
            Category: "heritage",
            ShortDescription: "A red-brick colonial church from the 1840s, standing just steps from the Temple of the Tooth.",
            Description:
            [
                "St. Paul’s Church was built in the mid-nineteenth century as a garrison church for the British in Kandy. Its red-brick exterior stands out among the whitewashed buildings around the temple.",
                "Inside, memorial tablets and stained glass reflect the colonial history of the city.",
            ],
            ImagePath: Img.StPauls,
            ImageAlt: "The red-brick facade of St. Paul’s Church in Kandy",
            Gallery: [],
            Address: "Near the Temple of the Sacred Tooth Relic, Kandy",
            Area: "Kandy City",
            Latitude: 7.2945,
            Longitude: 80.6397,
            CoordinatesApproximate: false,
            EstimatedVisitDurationMinutes: 30,
            OpeningHours: "Varies; may be closed outside service times (demo estimate)",
            EntranceFee: null,
            VisitorTips: ["Dress respectfully, especially during services."],
            Featured: false,
            Tags: ["church", "colonial", "architecture", "Anglican"]),
    ];
}
