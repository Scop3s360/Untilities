# 📈 Portfolio Command Center

![Python](https://img.shields.io/badge/Python-3.x-blue?logo=python)
![Status](https://img.shields.io/badge/status-active-success)
![License](https://img.shields.io/badge/license-MIT-green)
![UI](https://img.shields.io/badge/UI-Tkinter-orange)

A lightweight desktop app to track investments, analyse performance, and generate actionable signals — built for simplicity, speed, and clarity.

---

## 🚀 Features

* 💰 Track investments using **£ invested (no shares required)**
* 📊 Live data via Yahoo Finance
* 📈 Performance insights:

  * Current value
  * Profit / Loss (P/L)
  * Weekly & Monthly momentum
* 🔮 Estimated short-term impact (£)
* 🧠 Smart signal engine:

  * 🚀 **Add** → strong trend
  * 💀 **Exit** → weakening trend
  * 📈 **Hold** → performing well
  * ⚠️ **Review** → unclear
* 🎨 Clean, color-coded interface
* 🗂️ Automatic daily logging (`history.csv`)

---

## 🖥️ Preview

> *(Add a screenshot here later — makes this look 🔥)*

---

## 🧱 Input Format

```
TICKER:£INVESTED
```

### Example:

```
VWCE.DE:950, TTD:35, MDB:185, DE:370, HLMA.L:50
```

---

## ▶️ Getting Started

### 1. Clone the repo

```
git clone https://github.com/Scop3s360/ptracker.git
cd ptracker
```

### 2. Install dependencies

```
pip install yfinance
```

### 3. Run the app

```
python app.py
```

---

## 📁 Data Output

The app creates:

```
history.csv
```

Tracks:

* Date
* Ticker
* Invested amount
* Current value
* Profit/Loss
* Weekly/Monthly trends
* Signal generated

---

## 🧠 Strategy Philosophy

This tool is designed to:

* Identify **momentum early**
* Cut **weak positions quickly**
* Reinforce **winning investments**
* Reduce emotional decision-making

⚠️ Not financial advice — this is a decision-support tool, not a prediction engine.

---

## 🔮 Roadmap

* 📊 Portfolio performance graphs
* 🏆 Rank best opportunities
* 🎯 Signal accuracy tracking
* 📰 News + sentiment integration
* ☁️ Optional cloud sync

---

## ⚡ Vision

> From tracker → to decision engine → to personal investing system.

---

## 👤 Author

Built by **Scop3s**
Focused on building systems that compound decision quality over time.
