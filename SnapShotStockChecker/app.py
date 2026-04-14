# -*- coding: utf-8 -*-

import tkinter as tk
import yfinance as yf
import csv
from datetime import datetime

def run_scan():
    output.delete(1.0, tk.END)

    user_input = entry.get()
    stocks = []

    for s in user_input.split(","):
        parts = s.strip().split(":")
        if len(parts) < 3:
            continue
        ticker = parts[0].upper()
        invested = float(parts[1])
        buy_date = parts[2]
        stocks.append((ticker, invested, buy_date))

    today = datetime.now().strftime("%Y-%m-%d")

    total_value = 0
    total_invested = 0

    output.insert(tk.END, "PORTFOLIO RESULTS\n", "title")
    output.insert(tk.END, "====================================\n\n")

    with open("history.csv", "a", newline="", encoding="utf-8") as file:
        writer = csv.writer(file)

        for ticker, invested, buy_date in stocks:
            try:
                stock = yf.Ticker(ticker)

                # --- YOUR PERFORMANCE ---
                hist = stock.history(start=buy_date)

                if hist.empty:
                    output.insert(tk.END, f"{ticker} | No data since {buy_date}\n\n", "review")
                    continue

                latest = hist["Close"].iloc[-1]
                start_price = hist["Close"].iloc[0]

                total_return = (latest - start_price) / start_price
                current_value = invested * (1 + total_return)
                profit = current_value - invested

                total_value += current_value
                total_invested += invested

                # --- MARKET CONTEXT ---
                full_hist = stock.history(period="1y")

                if len(full_hist) < 30:
                    output.insert(tk.END, f"{ticker} | Not enough data\n\n", "review")
                    continue

                week = (latest - full_hist["Close"].iloc[-5]) / full_hist["Close"].iloc[-5]
                month = (latest - full_hist["Close"].iloc[-21]) / full_hist["Close"].iloc[-21]

                # --- GUARDRAILS ---
                overextended = week > 0.15
                crashing = week < -0.15

                # --- SIGNAL ENGINE ---
                if week > 0.05 and month > 0.10 and not overextended:
                    signal = "STRONG → Add (controlled)"
                    tag = "strong"

                elif overextended:
                    signal = "SPIKE → Do NOT chase"
                    tag = "review"

                elif crashing:
                    signal = "CRASHING → Consider EXIT"
                    tag = "sell"

                elif week < -0.05 and month < -0.10:
                    signal = "WEAK TREND → Reduce / Exit"
                    tag = "sell"

                elif profit > 0:
                    signal = "WINNING → Hold"
                    tag = "hold"

                else:
                    signal = "UNCLEAR → Review"
                    tag = "review"

                # --- IMPACT ---
                impact = current_value * week

                def fmt(val):
                    return f"{val*100:.2f}%"

                # --- OUTPUT ---
                output.insert(tk.END, f"{ticker}\n", "title")

                output.insert(tk.END, f"Invested: £{invested:.2f}\n")
                output.insert(tk.END, f"Buy Date: {buy_date}\n")
                output.insert(tk.END, f"Value: £{current_value:.2f}\n")

                pl_tag = "strong" if profit > 0 else "sell"
                output.insert(tk.END, f"P/L: £{profit:.2f}\n", pl_tag)

                output.insert(tk.END, f"Weekly: {fmt(week)} | Monthly: {fmt(month)}\n")

                impact_tag = "strong" if impact > 0 else "sell"
                output.insert(tk.END, f"Impact (est): £{impact:.2f}\n", impact_tag)

                output.insert(tk.END, f"{signal}\n\n", tag)

                # --- SAVE ---
                writer.writerow([
                    today,
                    ticker,
                    invested,
                    buy_date,
                    current_value,
                    profit,
                    week,
                    month,
                    impact,
                    signal
                ])

            except Exception as e:
                output.insert(tk.END, f"{ticker} | Error: {str(e)}\n\n", "review")

    total_profit = total_value - total_invested

    output.insert(tk.END, "====================================\n", "title")
    output.insert(tk.END, f"Total Invested: £{total_invested:.2f}\n")
    output.insert(tk.END, f"Total Value: £{total_value:.2f}\n")

    total_tag = "strong" if total_profit > 0 else "sell"
    output.insert(tk.END, f"Total P/L: £{total_profit:.2f}\n", total_tag)


# --- UI ---
root = tk.Tk()
root.title("Portfolio Command Center")
root.geometry("1000x750")
root.configure(bg="#0f172a")

title = tk.Label(
    root,
    text="Portfolio Command Center",
    fg="#38bdf8",
    bg="#0f172a",
    font=("Segoe UI", 18, "bold")
)
title.pack(pady=15)

label = tk.Label(
    root,
    text="Format: TICKER:£INVESTED:YYYY-MM-DD",
    fg="white",
    bg="#0f172a"
)
label.pack()

entry = tk.Entry(
    root,
    width=90,
    bg="#020617",
    fg="white",
    insertbackground="white",
    font=("Consolas", 10)
)
entry.pack(pady=10)

entry.insert(0, "VWCE.DE:950:2026-04-07, TTD:35:2026-04-07, MDB:185:2026-04-07, DE:370:2026-04-07, HLMA.L:50:2026-04-07, ONT.L:110:2026-04-07, ITM.L:100:2026-04-07, GNS.L:105:2026-04-07")

button = tk.Button(
    root,
    text="RUN ANALYSIS",
    command=run_scan,
    bg="#38bdf8",
    fg="black",
    font=("Segoe UI", 11, "bold"),
    padx=10,
    pady=5
)
button.pack(pady=10)

output = tk.Text(
    root,
    height=35,
    width=120,
    bg="#020617",
    fg="white",
    font=("Consolas", 10),
    bd=0
)
output.pack(pady=10)

# --- STYLES ---
output.tag_config("title", font=("Segoe UI", 11, "bold"))
output.tag_config("strong", foreground="#22c55e", font=("Consolas", 10, "bold"))
output.tag_config("sell", foreground="#ef4444", font=("Consolas", 10, "bold"))
output.tag_config("hold", foreground="#38bdf8", font=("Consolas", 10, "bold"))
output.tag_config("review", foreground="#f59e0b", font=("Consolas", 10, "bold"))

root.mainloop()