"""
rule_engine/defaults.py
=======================
Built-in categories and keyword rules.

Nothing here is hardcoded elsewhere in the application.
Categories are seeded into the DB once; the DB is the source of truth.
Rules are seeded at priority=100 so user rules (priority=0) always win.

To add a new built-in rule: append to BUILTIN_RULES.
To add a new category:      append to DEFAULT_CATEGORIES.
"""

from __future__ import annotations


# ── Categories ─────────────────────────────────────────────────────────────────
# List of (name, display_order) tuples.
# display_order controls dropdown / report ordering in the UI.

DEFAULT_CATEGORIES: list[tuple[str, int]] = [
    ("Income",          10),
    ("Salary",          11),
    ("Benefits",        12),
    ("Transfers",       20),
    ("Savings",         30),
    ("Mortgage",        40),
    ("Rent",            41),
    ("Council Tax",     50),
    ("Utilities",       60),
    ("Internet",        61),
    ("Phone",           62),
    ("Insurance",       70),
    ("Fuel",            80),
    ("Transport",       81),
    ("Parking",         82),
    ("Groceries",       90),
    ("Takeaways",       100),
    ("Restaurants",     101),
    ("Coffee",          102),
    ("Shopping",        110),
    ("Clothing",        111),
    ("Subscriptions",   120),
    ("Entertainment",   130),
    ("Health",          140),
    ("Medical",         141),
    ("Pets",            150),
    ("Education",       160),
    ("Travel",          170),
    ("Cash Withdrawal", 180),
    ("Fees",            190),
    ("Investments",     200),
    ("Gifts",           210),
    ("Other",           999),   # fallback — always last
]


# ── Built-in rules ─────────────────────────────────────────────────────────────
# List of (keyword, category_name) tuples.
# Keywords are matched case-insensitively as substrings of normalised
# transaction descriptions.  Order within this list doesn't matter for
# correctness — priority=100 is assigned to all built-in rules and they
# are sorted alphabetically by keyword length (longest first) at load time
# to ensure more-specific rules beat shorter ones when two could match.

BUILTIN_RULES: list[tuple[str, str]] = [
    # ── Groceries ────────────────────────────────────────────────────────
    ("TESCO",           "Groceries"),
    ("SAINSBURYS",      "Groceries"),
    ("SAINSBURY",       "Groceries"),
    ("ASDA",            "Groceries"),
    ("ALDI",            "Groceries"),
    ("LIDL",            "Groceries"),
    ("MORRISONS",       "Groceries"),
    ("WAITROSE",        "Groceries"),
    ("MARKS AND SPENCER FOOD", "Groceries"),
    ("M AND S FOOD",    "Groceries"),
    ("CO-OP FOOD",      "Groceries"),
    ("COOP",            "Groceries"),
    ("ICELAND",         "Groceries"),

    # ── Transport ────────────────────────────────────────────────────────
    ("UBER",            "Transport"),
    ("STAGECOACH",      "Transport"),
    ("TRAINLINE",       "Transport"),
    ("LNER",            "Transport"),
    ("LNE RAILWAY",     "Transport"),
    ("BLACK TAXI",      "Transport"),
    ("BLACKTAXI",       "Transport"),
    ("NATIONAL RAIL",   "Transport"),
    ("GREATER ANGLIA",  "Transport"),
    ("SOUTHERN RAIL",   "Transport"),
    ("ARRIVA",          "Transport"),
    ("FIRST GROUP",     "Transport"),
    ("TRANSLINK",       "Transport"),
    ("TFL",             "Transport"),
    ("TRANSPORT FOR LONDON", "Transport"),
    ("CITY MAPPER",     "Transport"),
    ("BOLT",            "Transport"),

    # ── Takeaways ────────────────────────────────────────────────────────
    ("DELIVEROO",       "Takeaways"),
    ("JUST EAT",        "Takeaways"),
    ("DOMINO",          "Takeaways"),
    ("DOMINOS",         "Takeaways"),
    ("PIZZA HUT",       "Takeaways"),
    ("MCDONALDS",       "Takeaways"),
    ("MCDONALD",        "Takeaways"),
    ("KFC",             "Takeaways"),
    ("SUBWAY",          "Takeaways"),
    ("FIVE GUYS",       "Takeaways"),
    ("BURGER KING",     "Takeaways"),
    ("NANDOS",          "Takeaways"),
    ("PAPA JOHN",       "Takeaways"),
    ("HUNGRY HOUSE",    "Takeaways"),
    ("UBER EATS",       "Takeaways"),

    # ── Coffee ───────────────────────────────────────────────────────────
    ("CAFFE NERO",      "Coffee"),
    ("STARBUCKS",       "Coffee"),
    ("COSTA",           "Coffee"),
    ("CAF LOCAL",       "Coffee"),
    ("PRET",            "Coffee"),
    ("GREGGS",          "Coffee"),     # primarily a coffee/bakery grab
    ("COFFEE",          "Coffee"),

    # ── Subscriptions ────────────────────────────────────────────────────
    ("NETFLIX",         "Subscriptions"),
    ("SPOTIFY",         "Subscriptions"),
    ("GOOGLE PLAY",     "Subscriptions"),
    ("APPLE.COM",       "Subscriptions"),
    ("APPLE COM",       "Subscriptions"),
    ("MICROSOFT",       "Subscriptions"),
    ("LINKEDIN",        "Subscriptions"),
    ("DISNEY",          "Subscriptions"),
    ("PRIME VIDEO",     "Subscriptions"),
    ("NOW TV",          "Subscriptions"),
    ("NOWTV",           "Subscriptions"),
    ("AUDIBLE",         "Subscriptions"),
    ("YOUTUBE",         "Subscriptions"),
    ("ADOBE",           "Subscriptions"),
    ("DROPBOX",         "Subscriptions"),
    ("ICLOUD",          "Subscriptions"),
    ("GITHUB",          "Subscriptions"),
    ("UBER ONE",        "Subscriptions"),

    # ── Fuel ─────────────────────────────────────────────────────────────
    ("SHELL",           "Fuel"),
    ("BP",              "Fuel"),
    ("ESSO",            "Fuel"),
    ("TEXACO",          "Fuel"),
    ("GULF",            "Fuel"),
    ("TOTAL ENERGY",    "Fuel"),
    ("MOTO",            "Fuel"),
    ("ROADCHEF",        "Fuel"),

    # ── Shopping ─────────────────────────────────────────────────────────
    ("AMAZON",          "Shopping"),
    ("EBAY",            "Shopping"),
    ("FIVERR",          "Shopping"),
    ("ASOS",            "Shopping"),
    ("ARGOS",           "Shopping"),
    ("JOHN LEWIS",      "Shopping"),
    ("VERY",            "Shopping"),
    ("NEXT",            "Shopping"),
    ("DUNELM",          "Shopping"),
    ("IKEA",            "Shopping"),
    ("ETSY",            "Shopping"),
    ("SHEIN",           "Shopping"),

    # ── Clothing ─────────────────────────────────────────────────────────
    ("PRIMARK",         "Clothing"),
    ("H AND M",         "Clothing"),
    ("ZARA",            "Clothing"),
    ("MARKS AND SPENCER", "Clothing"),
    ("M AND S",         "Clothing"),
    ("RIVER ISLAND",    "Clothing"),
    ("TOPSHOP",         "Clothing"),
    ("BOOHOO",          "Clothing"),

    # ── Savings ──────────────────────────────────────────────────────────
    ("SPRIVE",          "Savings"),
    ("MONEYBOX",        "Savings"),
    ("NUTMEG",          "Savings"),
    ("VANGUARD",        "Savings"),

    # ── Investments ──────────────────────────────────────────────────────
    ("TRADING 212",     "Investments"),
    ("FREETRADE",       "Investments"),
    ("HARGREAVES",      "Investments"),
    ("AJ BELL",         "Investments"),

    # ── Income ───────────────────────────────────────────────────────────
    ("AMAZON WEB SERVICE", "Income"),   # AWS salary/freelance credit
    ("HMRC",            "Income"),
    ("UNIVERSAL CREDIT", "Benefits"),
    ("DWP",             "Benefits"),

    # ── Utilities ────────────────────────────────────────────────────────
    ("BRITISH GAS",     "Utilities"),
    ("EDF",             "Utilities"),
    ("EON",             "Utilities"),
    ("OCTOPUS",         "Utilities"),
    ("BULB",            "Utilities"),
    ("SCOTTISH POWER",  "Utilities"),
    ("THAMES WATER",    "Utilities"),
    ("SEVERN TRENT",    "Utilities"),
    ("ANGLIAN WATER",   "Utilities"),

    # ── Internet / Phone ─────────────────────────────────────────────────
    ("BT GROUP",        "Internet"),
    ("SKY",             "Internet"),
    ("VIRGIN MEDIA",    "Internet"),
    ("VM PERSONAL",     "Internet"),
    ("TALKTALK",        "Internet"),
    ("VODAFONE",        "Phone"),
    ("O2",              "Phone"),
    ("EE",              "Phone"),
    ("THREE",           "Phone"),
    ("GIFFGAFF",        "Phone"),

    # ── Insurance ────────────────────────────────────────────────────────
    ("INSURANCE",       "Insurance"),
    ("AVIVA",           "Insurance"),
    ("AXA",             "Insurance"),
    ("LEGAL AND GENERAL", "Insurance"),
    ("DIRECT LINE",     "Insurance"),

    # ── Health / Medical ─────────────────────────────────────────────────
    ("BOOTS",           "Health"),
    ("SUPERDRUG",       "Health"),
    ("PHARMACY",        "Medical"),
    ("DENTIST",         "Medical"),
    ("DENTAL",          "Medical"),
    ("NHS",             "Medical"),
    ("OPTICIAN",        "Medical"),
    ("OPTICAL",         "Medical"),
    ("AO OPTICAL",      "Medical"),

    # ── Entertainment ────────────────────────────────────────────────────
    ("CINEMA",          "Entertainment"),
    ("ODEON",           "Entertainment"),
    ("VUE",             "Entertainment"),
    ("CINEWORLD",       "Entertainment"),
    ("TICKETMASTER",    "Entertainment"),
    ("EVENTBRITE",      "Entertainment"),
    ("STEAM",           "Entertainment"),
    ("PLAYSTATION",     "Entertainment"),
    ("XBOX",            "Entertainment"),

    # ── Travel ───────────────────────────────────────────────────────────
    ("BOOKING.COM",     "Travel"),
    ("BOOKING COM",     "Travel"),
    ("AIRBNB",          "Travel"),
    ("EXPEDIA",         "Travel"),
    ("HOLIDAY INN",     "Travel"),
    ("PREMIER INN",     "Travel"),
    ("TRAVELODGE",      "Travel"),
    ("RYANAIR",         "Travel"),
    ("EASYJET",         "Travel"),
    ("BRITISH AIRWAYS", "Travel"),
    ("EUROSTAR",        "Travel"),

    # ── Council Tax ──────────────────────────────────────────────────────
    ("COUNCIL TAX",     "Council Tax"),
    ("COUNCIL",         "Council Tax"),

    # ── Cash Withdrawal ──────────────────────────────────────────────────
    ("CASH WITHDRAWAL", "Cash Withdrawal"),
    ("ATM",             "Cash Withdrawal"),
    ("CASHPOINT",       "Cash Withdrawal"),

    # ── Fees ─────────────────────────────────────────────────────────────
    ("BANK CHARGE",     "Fees"),
    ("OVERDRAFT",       "Fees"),
    ("LATE PAYMENT",    "Fees"),

    # ── Education ────────────────────────────────────────────────────────
    ("UDEMY",           "Education"),
    ("COURSERA",        "Education"),
    ("PLURALSIGHT",     "Education"),
    ("DUOLINGO",        "Education"),
    ("SCHOOL",          "Education"),
    ("UNIVERSITY",      "Education"),
    ("COLLEGE",         "Education"),
]
