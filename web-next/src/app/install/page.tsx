"use client";

import { useState } from "react";

const tabs = ["iOS Safari", "Android Chrome", "Desktop Chrome"] as const;

const steps: Record<string, { title: string; desc: string }[]> = {
  "iOS Safari": [
    { title: "Open scorecast.uk in Safari", desc: "Tap the Share button (box with arrow) at the bottom of the screen." },
    { title: "Tap 'Add to Home Screen'", desc: "Scroll down in the share menu and tap 'Add to Home Screen', then tap Add." },
    { title: "You're all set! 🎉", desc: "ScoreCast will appear on your home screen as a regular app." },
  ],
  "Android Chrome": [
    { title: "Open scorecast.uk in Chrome", desc: "Tap the three-dot menu (⋮) in the top right corner." },
    { title: "Tap 'Add to Home Screen' or 'Install App'", desc: "Confirm by tapping Add or Install." },
    { title: "You're all set! 🎉", desc: "ScoreCast will appear in your app drawer and home screen." },
  ],
  "Desktop Chrome": [
    { title: "Open scorecast.uk in Chrome", desc: "Look for the install icon (⊕) in the address bar, or go to Menu → Install ScoreCast." },
    { title: "Click Install", desc: "Confirm the installation prompt." },
    { title: "You're all set! 🎉", desc: "ScoreCast opens as a standalone window — no browser chrome." },
  ],
};

export default function InstallPage() {
  const [tab, setTab] = useState<string>("iOS Safari");

  return (
    <div className="max-w-md mx-auto px-4 py-6 text-white pb-24">
      <div className="text-center mb-6">
        <h1 className="text-2xl font-extrabold">Install ScoreCast</h1>
        <p className="text-sm opacity-60">Add ScoreCast to your home screen for the best experience</p>
      </div>

      <div className="bg-[#FFF3E0] text-[#333] rounded-lg border-l-4 border-[#FF6B35] p-3 text-sm mb-4">
        <strong className="text-[#FF6B35]">📱 Why not a regular app?</strong><br />
        Native apps are in development. During beta, install ScoreCast as a web app — it works just like a native app.
      </div>

      {/* Tabs */}
      <div className="flex gap-1 mb-6 bg-white/5 rounded-lg p-1">
        {tabs.map(t => (
          <button key={t} onClick={() => setTab(t)}
            className={`flex-1 py-2 rounded-md text-xs font-bold transition ${t === tab ? "bg-[#FF6B35]" : "opacity-60"}`}>
            {t}
          </button>
        ))}
      </div>

      {/* Steps */}
      <div className="space-y-6">
        {steps[tab].map((s, i) => (
          <div key={i}>
            <div className="flex items-center gap-2 mb-1">
              <span className="w-8 h-8 rounded-full bg-[#FF6B35] flex items-center justify-center text-sm font-extrabold">{i + 1}</span>
              <span className="font-bold">{s.title}</span>
            </div>
            <p className="text-sm opacity-70 ml-10">{s.desc}</p>
            <div className="ml-10 mt-2 h-40 bg-white/5 rounded-xl flex items-center justify-center text-xs opacity-40">
              Screenshot placeholder
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
