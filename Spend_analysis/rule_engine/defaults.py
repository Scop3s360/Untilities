"""
rule_engine/defaults.py
=======================
Built-in categories and keyword rules.

All existing categories and rules are preserved.
New additions are clearly marked # NEW.

Categories seed with INSERT OR IGNORE — existing DB records are never overwritten.
Rules seed with INSERT OR IGNORE — existing DB records and all user rules are safe.
"""

from __future__ import annotations


# ── Categories ─────────────────────────────────────────────────────────────────
# (name, display_order)  — display_order controls UI sort order.
# INSERT OR IGNORE means existing DB entries are never changed.

DEFAULT_CATEGORIES: list[tuple[str, int]] = [
    # ── Income ──────────────────────────────────────────────────────────
    ("Income",              10),
    ("Salary",              11),
    ("Benefits",            12),
    ("Refunds",             13),       # NEW
    ("Gifts Received",      14),       # NEW
    ("Interest",            15),       # NEW
    ("Other Income",        19),       # NEW

    # ── Home ────────────────────────────────────────────────────────────
    ("Home",                20),       # NEW — group header
    ("Mortgage",            21),
    ("Rent",                22),
    ("Council Tax",         23),
    ("Utilities",           24),
    ("Internet",            25),
    ("Phone",               26),
    ("Insurance",           27),
    ("Home Maintenance",    28),       # NEW

    # ── Living ──────────────────────────────────────────────────────────
    ("Living",              30),       # NEW — group header
    ("Groceries",           31),
    ("Takeaways",           32),
    ("Restaurants",         33),
    ("Coffee",              34),
    ("Alcohol",             35),       # NEW

    # ── Transport ───────────────────────────────────────────────────────
    ("Transport",           40),
    ("Fuel",                41),
    ("Public Transport",    42),       # NEW
    ("Taxi / Uber",         43),       # NEW
    ("Parking",             44),
    ("Vehicle Maintenance", 45),       # NEW

    # ── Shopping ────────────────────────────────────────────────────────
    ("Shopping",            50),
    ("General Shopping",    51),       # NEW
    ("Clothing",            52),
    ("Electronics",         53),       # NEW
    ("Hobbies",             54),       # NEW
    ("Books",               55),       # NEW
    ("DIY",                 56),       # NEW

    # ── Lifestyle ───────────────────────────────────────────────────────
    ("Lifestyle",           60),       # NEW — group header
    ("Entertainment",       61),
    ("Subscriptions",       62),
    ("Gaming",              63),       # NEW
    ("Holiday",             64),       # NEW
    ("Travel",              65),
    ("Hotels",              66),       # NEW
    ("Flights",             67),       # NEW

    # ── Family ──────────────────────────────────────────────────────────
    ("Family",              70),       # NEW — group header
    ("Child Support",       71),       # NEW
    ("Childcare",           72),       # NEW
    ("Education",           73),
    ("Pets",                74),
    ("Neon Goblin",         75),       # NEW — personal hobby category
    ("Gifts",               76),

    # ── Health ──────────────────────────────────────────────────────────
    ("Health",              80),
    ("Medical",             81),
    ("Dental",              82),       # NEW
    ("Pharmacy",            83),       # NEW
    ("Fitness",             84),       # NEW

    # ── Finance ─────────────────────────────────────────────────────────
    ("Finance",             90),       # NEW — group header
    ("Savings",             91),
    ("Investments",         92),
    ("Bank Fees",           93),       # NEW
    ("Fees",                94),       # preserved (legacy label)
    ("Cash Withdrawal",     95),
    ("Transfers",           96),

    # ── Other ────────────────────────────────────────────────────────────
    ("Charity",             97),       # NEW
    ("Unknown",             998),      # NEW — explicit unknown
    ("Other",               999),      # fallback — always last
]


# ── Built-in rules ─────────────────────────────────────────────────────────────
# (keyword, category_name)
# Matched case-insensitively as substrings of normalised descriptions.
# Sorted longest-keyword-first at load time for specificity.
# All entries use INSERT OR IGNORE — existing DB rules are never changed.

BUILTIN_RULES: list[tuple[str, str]] = [

    # ── Groceries ────────────────────────────────────────────────────────
    ("TESCO",                   "Groceries"),
    ("SAINSBURYS",              "Groceries"),
    ("SAINSBURY",               "Groceries"),
    ("ASDA",                    "Groceries"),
    ("ALDI",                    "Groceries"),
    ("LIDL",                    "Groceries"),
    ("MORRISONS",               "Groceries"),
    ("WAITROSE",                "Groceries"),
    ("MARKS AND SPENCER FOOD",  "Groceries"),
    ("M AND S FOOD",            "Groceries"),
    ("CO-OP FOOD",              "Groceries"),
    ("COOP",                    "Groceries"),
    ("ICELAND",                 "Groceries"),

    # ── Transport (general) ───────────────────────────────────────────────
    ("STAGECOACH",              "Public Transport"),  # NEW category
    ("TRAINLINE",               "Public Transport"),  # NEW category
    ("LNER",                    "Public Transport"),  # NEW category
    ("LNE RAILWAY",             "Public Transport"),  # NEW category
    ("NATIONAL RAIL",           "Public Transport"),  # NEW category
    ("GREATER ANGLIA",          "Public Transport"),  # NEW category
    ("SOUTHERN RAIL",           "Public Transport"),  # NEW category
    ("ARRIVA",                  "Public Transport"),  # NEW category
    ("FIRST GROUP",             "Public Transport"),  # NEW category
    ("TRANSLINK",               "Public Transport"),  # NEW category
    ("TFL",                     "Public Transport"),  # NEW category
    ("TRANSPORT FOR LONDON",    "Public Transport"),  # NEW category
    ("CITY MAPPER",             "Public Transport"),  # NEW category
    ("EUROSTAR",                "Public Transport"),  # NEW

    # ── Taxi / Uber ────────────────────────────────────────────────────────
    ("UBER",                    "Taxi / Uber"),        # NEW category
    ("BOLT",                    "Taxi / Uber"),        # NEW category
    ("BLACK TAXI",              "Taxi / Uber"),        # NEW category
    ("BLACKTAXI",               "Taxi / Uber"),        # NEW category
    ("ADDISON LEE",             "Taxi / Uber"),        # NEW

    # ── Fuel ──────────────────────────────────────────────────────────────
    ("SHELL",                   "Fuel"),
    ("ESSO",                    "Fuel"),
    ("TEXACO",                  "Fuel"),
    ("GULF",                    "Fuel"),
    ("TOTAL ENERGY",            "Fuel"),
    ("MOTO",                    "Fuel"),
    ("ROADCHEF",                "Fuel"),
    ("BP",                      "Fuel"),

    # ── Parking ───────────────────────────────────────────────────────────
    ("PARKING",                 "Parking"),            # NEW
    ("NCP",                     "Parking"),            # NEW
    ("RINGO",                   "Parking"),            # NEW
    ("PAYBYPHONE",              "Parking"),            # NEW

    # ── Vehicle Maintenance ────────────────────────────────────────────────
    ("HALFORDS",                "Vehicle Maintenance"), # NEW
    ("KWIK FIT",                "Vehicle Maintenance"), # NEW
    ("KWIKFIT",                 "Vehicle Maintenance"), # NEW

    # ── Takeaways ─────────────────────────────────────────────────────────
    ("DELIVEROO",               "Takeaways"),
    ("JUST EAT",                "Takeaways"),
    ("DOMINO",                  "Takeaways"),
    ("DOMINOS",                 "Takeaways"),
    ("PIZZA HUT",               "Takeaways"),
    ("MCDONALDS",               "Takeaways"),
    ("MCDONALD",                "Takeaways"),
    ("KFC",                     "Takeaways"),
    ("SUBWAY",                  "Takeaways"),
    ("FIVE GUYS",               "Takeaways"),
    ("BURGER KING",             "Takeaways"),
    ("NANDOS",                  "Takeaways"),
    ("PAPA JOHN",               "Takeaways"),
    ("HUNGRY HOUSE",            "Takeaways"),
    ("UBER EATS",               "Takeaways"),

    # ── Coffee ────────────────────────────────────────────────────────────
    ("CAFFE NERO",              "Coffee"),
    ("STARBUCKS",               "Coffee"),
    ("COSTA",                   "Coffee"),
    ("CAF LOCAL",               "Coffee"),
    ("PRET",                    "Coffee"),
    ("GREGGS",                  "Coffee"),
    ("COFFEE",                  "Coffee"),
    ("CAFE",                    "Coffee"),             # NEW

    # ── Restaurants ───────────────────────────────────────────────────────
    ("RESTAURANT",              "Restaurants"),        # NEW
    ("ZIZZI",                   "Restaurants"),        # NEW
    ("WAGAMAMA",                "Restaurants"),        # NEW
    ("FRANKIE AND BENNY",       "Restaurants"),        # NEW
    ("PIZZA EXPRESS",           "Restaurants"),        # NEW
    ("TGI FRIDAY",              "Restaurants"),        # NEW
    ("HARVESTER",               "Restaurants"),        # NEW

    # ── Alcohol ───────────────────────────────────────────────────────────
    ("WETHERSPOON",             "Alcohol"),            # NEW
    ("WEATHERSPOON",            "Alcohol"),            # NEW
    ("STONEGATE",               "Alcohol"),            # NEW

    # ── Subscriptions ─────────────────────────────────────────────────────
    ("NETFLIX",                 "Subscriptions"),
    ("SPOTIFY",                 "Subscriptions"),
    ("GOOGLE PLAY",             "Subscriptions"),
    ("APPLE.COM",               "Subscriptions"),
    ("APPLE COM",               "Subscriptions"),
    ("MICROSOFT",               "Subscriptions"),
    ("LINKEDIN",                "Subscriptions"),
    ("DISNEY",                  "Subscriptions"),
    ("PRIME VIDEO",             "Subscriptions"),
    ("NOW TV",                  "Subscriptions"),
    ("NOWTV",                   "Subscriptions"),
    ("AUDIBLE",                 "Subscriptions"),
    ("YOUTUBE",                 "Subscriptions"),
    ("ADOBE",                   "Subscriptions"),
    ("DROPBOX",                 "Subscriptions"),
    ("ICLOUD",                  "Subscriptions"),
    ("GITHUB",                  "Subscriptions"),
    ("UBER ONE",                "Subscriptions"),
    ("AMAZON PRIME",            "Subscriptions"),      # NEW

    # ── Gaming ────────────────────────────────────────────────────────────
    ("STEAM",                   "Gaming"),             # NEW category (was Entertainment)
    ("PLAYSTATION",             "Gaming"),             # NEW category (was Entertainment)
    ("XBOX",                    "Gaming"),             # NEW category (was Entertainment)
    ("EPIC GAMES",              "Gaming"),             # NEW
    ("NINTENDO",                "Gaming"),             # NEW
    ("HUMBLE BUNDLE",           "Gaming"),             # NEW
    ("GOG.COM",                 "Gaming"),             # NEW

    # ── Entertainment ─────────────────────────────────────────────────────
    ("CINEMA",                  "Entertainment"),
    ("ODEON",                   "Entertainment"),
    ("VUE",                     "Entertainment"),
    ("CINEWORLD",               "Entertainment"),
    ("TICKETMASTER",            "Entertainment"),
    ("EVENTBRITE",              "Entertainment"),
    ("SEE TICKETS",             "Entertainment"),      # NEW
    ("SKIDDLE",                 "Entertainment"),      # NEW

    # ── Holiday ───────────────────────────────────────────────────────────
    ("EASYJET",                 "Holiday"),            # NEW (was Travel)
    ("RYANAIR",                 "Holiday"),            # NEW (was Travel)
    ("JET2",                    "Holiday"),            # NEW
    ("TUI",                     "Holiday"),            # NEW
    ("BRITISH AIRWAYS",         "Holiday"),            # NEW (was Travel)
    ("BOOKING.COM",             "Holiday"),            # NEW (was Travel)
    ("BOOKING COM",             "Holiday"),            # NEW (was Travel)
    ("AIRBNB",                  "Holiday"),            # NEW (was Travel)
    ("EXPEDIA",                 "Holiday"),            # NEW (was Travel)
    ("HOTELS.COM",              "Holiday"),            # NEW
    ("LAST MINUTE",             "Holiday"),            # NEW
    ("ON THE BEACH",            "Holiday"),            # NEW
    ("LOVEHOLIDAYS",            "Holiday"),            # NEW

    # ── Hotels ────────────────────────────────────────────────────────────
    ("HOLIDAY INN",             "Hotels"),             # NEW category
    ("PREMIER INN",             "Hotels"),             # NEW category
    ("TRAVELODGE",              "Hotels"),             # NEW category
    ("MARRIOTT",                "Hotels"),             # NEW
    ("HILTON",                  "Hotels"),             # NEW
    ("IBIS",                    "Hotels"),             # NEW

    # ── Flights ───────────────────────────────────────────────────────────
    ("WIZZ AIR",                "Flights"),            # NEW
    ("NORWEGIAN",               "Flights"),            # NEW

    # ── Travel (general) ──────────────────────────────────────────────────
    ("EUROCAR",                 "Travel"),             # NEW
    ("HERTZ",                   "Travel"),             # NEW
    ("ENTERPRISE CAR",          "Travel"),             # NEW

    # ── Hobbies ───────────────────────────────────────────────────────────
    ("GAMES WORKSHOP",          "Hobbies"),            # NEW
    ("WARHAMMER",               "Hobbies"),            # NEW
    ("HOBBYCRAFT",              "Hobbies"),            # NEW
    ("MODELZONE",               "Hobbies"),            # NEW

    # ── Neon Goblin ───────────────────────────────────────────────────────
    ("NEON GOBLIN",             "Neon Goblin"),        # NEW — personal

    # ── Clothing ──────────────────────────────────────────────────────────
    ("PRIMARK",                 "Clothing"),
    ("H AND M",                 "Clothing"),
    ("ZARA",                    "Clothing"),
    ("MARKS AND SPENCER",       "Clothing"),
    ("M AND S",                 "Clothing"),
    ("RIVER ISLAND",            "Clothing"),
    ("TOPSHOP",                 "Clothing"),
    ("BOOHOO",                  "Clothing"),
    ("NEXT",                    "Clothing"),           # NEW — was Shopping
    ("JD SPORTS",               "Clothing"),           # NEW
    ("SPORTS DIRECT",           "Clothing"),           # NEW
    ("NIKE",                    "Clothing"),           # NEW
    ("ADIDAS",                  "Clothing"),           # NEW
    ("FOOTLOCKER",              "Clothing"),           # NEW
    ("NEW LOOK",                "Clothing"),           # NEW
    ("DOROTHY PERKINS",         "Clothing"),           # NEW
    ("BURTON",                  "Clothing"),           # NEW

    # ── Electronics ───────────────────────────────────────────────────────
    ("CURRYS",                  "Electronics"),        # NEW
    ("PC WORLD",                "Electronics"),        # NEW
    ("APPLE STORE",             "Electronics"),        # NEW

    # ── General Shopping ──────────────────────────────────────────────────
    ("AMAZON",                  "General Shopping"),   # NEW category (was Shopping)
    ("EBAY",                    "General Shopping"),   # NEW category
    ("FIVERR",                  "General Shopping"),   # NEW category
    ("ASOS",                    "General Shopping"),   # NEW category
    ("ARGOS",                   "General Shopping"),
    ("JOHN LEWIS",              "General Shopping"),
    ("VERY",                    "General Shopping"),
    ("DUNELM",                  "General Shopping"),
    ("IKEA",                    "General Shopping"),
    ("ETSY",                    "General Shopping"),
    ("SHEIN",                   "General Shopping"),
    ("TEMU",                    "General Shopping"),   # NEW

    # ── Books ─────────────────────────────────────────────────────────────
    ("WATERSTONES",             "Books"),              # NEW
    ("KINDLE",                  "Books"),              # NEW
    ("WHSmith",                 "Books"),              # NEW
    ("THE WORKS",               "Books"),              # NEW

    # ── DIY ───────────────────────────────────────────────────────────────
    ("B AND Q",                 "DIY"),                # NEW
    ("SCREWFIX",                "DIY"),                # NEW
    ("TOOLSTATION",             "DIY"),                # NEW
    ("WICKES",                  "DIY"),                # NEW
    ("HOMEBASE",                "DIY"),                # NEW
    ("TRAVIS PERKINS",          "DIY"),                # NEW

    # ── Savings ───────────────────────────────────────────────────────────
    ("SPRIVE",                  "Savings"),
    ("MONEYBOX",                "Savings"),
    ("NUTMEG",                  "Savings"),
    ("VANGUARD",                "Savings"),
    ("CHIP",                    "Savings"),            # NEW

    # ── Investments ───────────────────────────────────────────────────────
    ("TRADING 212",             "Investments"),
    ("FREETRADE",               "Investments"),
    ("HARGREAVES",              "Investments"),
    ("AJ BELL",                 "Investments"),
    ("INTERACTIVE INVESTOR",    "Investments"),        # NEW

    # ── Income ────────────────────────────────────────────────────────────
    ("AMAZON WEB SERVICE",      "Income"),
    ("HMRC",                    "Income"),

    # ── Benefits ──────────────────────────────────────────────────────────
    ("UNIVERSAL CREDIT",        "Benefits"),
    ("DWP",                     "Benefits"),

    # ── Utilities ─────────────────────────────────────────────────────────
    ("BRITISH GAS",             "Utilities"),
    ("EDF",                     "Utilities"),
    ("EON",                     "Utilities"),
    ("OCTOPUS",                 "Utilities"),
    ("BULB",                    "Utilities"),
    ("SCOTTISH POWER",          "Utilities"),
    ("THAMES WATER",            "Utilities"),
    ("SEVERN TRENT",            "Utilities"),
    ("ANGLIAN WATER",           "Utilities"),
    ("SOUTHERN WATER",          "Utilities"),          # NEW

    # ── Internet ──────────────────────────────────────────────────────────
    ("BT GROUP",                "Internet"),
    ("SKY",                     "Internet"),
    ("VIRGIN MEDIA",            "Internet"),
    ("VM PERSONAL",             "Internet"),
    ("TALKTALK",                "Internet"),
    ("PLUSNET",                 "Internet"),           # NEW

    # ── Phone ─────────────────────────────────────────────────────────────
    ("VODAFONE",                "Phone"),
    ("O2",                      "Phone"),
    ("EE",                      "Phone"),
    ("THREE",                   "Phone"),
    ("GIFFGAFF",                "Phone"),
    ("SMARTY",                  "Phone"),              # NEW
    ("LEBARA",                  "Phone"),              # NEW

    # ── Insurance ─────────────────────────────────────────────────────────
    ("INSURANCE",               "Insurance"),
    ("AVIVA",                   "Insurance"),
    ("AXA",                     "Insurance"),
    ("LEGAL AND GENERAL",       "Insurance"),
    ("DIRECT LINE",             "Insurance"),
    ("ADMIRAL",                 "Insurance"),          # NEW
    ("COMPARE THE MARKET",      "Insurance"),          # NEW

    # ── Pharmacy ──────────────────────────────────────────────────────────
    ("BOOTS",                   "Pharmacy"),           # NEW category (was Health)
    ("SUPERDRUG",               "Pharmacy"),           # NEW category (was Health)
    ("PHARMACY",                "Pharmacy"),           # NEW category (was Medical)
    ("LLOYDS PHARMACY",         "Pharmacy"),           # NEW
    ("WELL PHARMACY",           "Pharmacy"),           # NEW

    # ── Dental ────────────────────────────────────────────────────────────
    ("DENTIST",                 "Dental"),             # NEW category (was Medical)
    ("DENTAL",                  "Dental"),             # NEW category (was Medical)

    # ── Medical ───────────────────────────────────────────────────────────
    ("NHS",                     "Medical"),
    ("OPTICIAN",                "Medical"),
    ("OPTICAL",                 "Medical"),
    ("AO OPTICAL",              "Medical"),
    ("SPECSAVERS",              "Medical"),            # NEW
    ("BUPA",                    "Medical"),            # NEW
    ("PRIVATE HEALTH",          "Medical"),            # NEW

    # ── Fitness ───────────────────────────────────────────────────────────
    ("PUREGYM",                 "Fitness"),            # NEW
    ("PURE GYM",                "Fitness"),            # NEW
    ("DAVID LLOYD",             "Fitness"),            # NEW
    ("VIRGIN ACTIVE",           "Fitness"),            # NEW
    ("FITNESS FIRST",           "Fitness"),            # NEW
    ("THE GYM GROUP",           "Fitness"),            # NEW
    ("GYM",                     "Fitness"),            # NEW

    # ── Education ─────────────────────────────────────────────────────────
    ("UDEMY",                   "Education"),
    ("COURSERA",                "Education"),
    ("PLURALSIGHT",             "Education"),
    ("DUOLINGO",                "Education"),
    ("SCHOOL",                  "Education"),
    ("UNIVERSITY",              "Education"),
    ("COLLEGE",                 "Education"),
    ("SKILLSHARE",              "Education"),          # NEW
    ("LINKEDIN LEARNING",       "Education"),          # NEW

    # ── Pets ──────────────────────────────────────────────────────────────
    ("PETS AT HOME",            "Pets"),               # NEW
    ("VET",                     "Pets"),               # NEW
    ("VETS",                    "Pets"),               # NEW
    ("PETPLAN",                 "Pets"),               # NEW

    # ── Charity ───────────────────────────────────────────────────────────
    ("JUSTGIVING",              "Charity"),            # NEW
    ("CHARITY",                 "Charity"),            # NEW
    ("COMIC RELIEF",            "Charity"),            # NEW
    ("RED CROSS",               "Charity"),            # NEW

    # ── Council Tax ───────────────────────────────────────────────────────
    ("COUNCIL TAX",             "Council Tax"),
    ("COUNCIL",                 "Council Tax"),

    # ── Cash Withdrawal ───────────────────────────────────────────────────
    ("CASH WITHDRAWAL",         "Cash Withdrawal"),
    ("ATM",                     "Cash Withdrawal"),
    ("CASHPOINT",               "Cash Withdrawal"),

    # ── Bank Fees ─────────────────────────────────────────────────────────
    ("BANK CHARGE",             "Bank Fees"),          # NEW category
    ("OVERDRAFT",               "Bank Fees"),          # NEW category
    ("LATE PAYMENT",            "Bank Fees"),          # NEW category
    ("ARRANGEMENT FEE",         "Bank Fees"),          # NEW

    # ── Transfers ─────────────────────────────────────────────────────────
    ("BANK TRANSFER",           "Transfers"),          # NEW
    ("FASTER PAYMENT",          "Transfers"),          # NEW

    # ── Refunds ───────────────────────────────────────────────────────────
    ("REFUND",                  "Refunds"),            # NEW
    ("CASHBACK",                "Refunds"),            # NEW
]
