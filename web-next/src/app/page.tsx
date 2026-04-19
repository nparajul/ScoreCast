"use client";

import Link from "next/link";
import { useAuth } from "@/contexts/auth-context";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";

export default function Home() {
  const { user, loading } = useAuth();
  const router = useRouter();
  const [openFaq, setOpenFaq] = useState<number | null>(null);

  useEffect(() => {
    if (!loading && user) router.replace("/dashboard");
  }, [user, loading, router]);

  if (loading || user) {
    return (
      <div className="flex items-center justify-center min-h-[60vh]">
        <div className="text-4xl animate-pulse">⚽</div>
      </div>
    );
  }

  const features = [
    { icon: "🏆", color: "tertiary", title: "Score Predictions", desc: "Predict exact scorelines for every match. Earn points for correct results, bonus for exact scores." },
    { icon: "⚽", color: "secondary", title: "Live Scores", desc: "Follow every match in real time — goals, cards, substitutions, and minute-by-minute updates." },
    { icon: "✨", color: "primary", title: "AI Insights", desc: "AI-generated match previews with form analysis, head-to-head stats, and predicted outcomes." },
    { icon: "👥", color: "secondary", title: "Private Leagues", desc: "Create a league, invite your mates, and compete head-to-head across the season." },
    { icon: "🎲", color: "purple", title: "Risk Plays", desc: "Feeling brave? Optional side bets each gameweek — Double Down, Exact Score Boost, Clean Sheet, and more." },
    { icon: "📊", color: "primary", title: "Tables & Stats", desc: "League tables, player stats, top scorers, form guides — everything to follow the season." },
    { icon: "🛡️", color: "tertiary", title: "Team Profiles", desc: "Detailed team pages with squads, recent results, upcoming fixtures, and competition history." },
  ];

  const faqs = [
    { q: "Is ScoreCast a betting or gambling site?", a: "No. ScoreCast is a free prediction game played purely for fun and bragging rights. No real money, no stakes, no wagers, no payouts. Terms like \"risk plays\" and \"bonus points\" refer only to in-game scoring mechanics with zero monetary value." },
    { q: "Is ScoreCast free?", a: "Yes — free, always and forever. No hidden fees, no catches." },
    { q: "Why is ScoreCast in beta?", a: "Built in spare time by one developer. Core features work, but you may run into rough edges. Report bugs on GitHub Issues — it really helps!" },
    { q: "When will ScoreCast be fully ready?", a: "Aiming for the FIFA World Cup 2026. At the latest, the Premier League 2026/27 season. Fully playable right now." },
    { q: "How does scoring work?", a: "Check out our How to Play page for the full scoring breakdown, risk plays guide, and everything else you need to know." },
    { q: "What are Risk Plays?", a: "Risk plays are optional side challenges each gameweek for bonus points (or penalties). See the full guide on our How to Play page." },
    { q: "Can I play with friends?", a: "Absolutely. Create a private league, share the invite code, compete. Join multiple leagues at once." },
    { q: "What competitions are supported?", a: "Premier League (full 38-matchweek season) and FIFA World Cup 2026 (groups + knockout). More coming." },
    { q: "What are AI Insights?", a: "Before each gameweek, our AI analyzes form, head-to-head records, and recent results to generate match previews and predicted outcomes." },
    { q: "Will my data be safe?", a: "Authentication is handled by Google Firebase. We don't store passwords. Your data is secure and never shared." },
    { q: "What stats and data are available?", a: "Player stats, team profiles, squad lists, recent form, and full season data — all inside the app." },
    { q: "Can I follow fixtures and match events live?", a: "Yes. Live scores, match timelines with goals, cards, and substitutions, plus upcoming fixtures and full results." },
  ];

  const poweredBy = [
    { icon: "⚽", title: "Premier League Pulse API", desc: "Live scores, fixtures, events & team data" },
    { icon: "💾", title: "Football-Data.org", desc: "Match data & competition coverage" },
    { icon: "👤", title: "FPL API", desc: "Player data & enrichment" },
    { icon: "🧠", title: "OpenAI", desc: "AI-powered match insights" },
    { icon: "🔒", title: "Firebase Auth", desc: "Secure authentication" },
    { icon: "☁️", title: "Cloudflare & Neon", desc: "Hosting & PostgreSQL database" },
  ];

  const colorClass = (c: string) => ({
    primary: "bg-[var(--sc-primary)]",
    secondary: "bg-[var(--sc-secondary)]",
    tertiary: "bg-[var(--sc-tertiary)]",
    purple: "bg-gradient-to-br from-[#7c4dff] to-[#651fff]",
  }[c] || "bg-[var(--sc-primary)]");

  return (
    <div className="py-6 space-y-14">
      {/* Hero */}
      <section className="flex flex-col items-center text-center px-4">
        <div className="text-5xl font-bold tracking-tight mb-2">⚽ ScoreCast</div>
        <span className="inline-block px-3 py-1 rounded-full border border-amber-500 text-amber-600 text-xs font-bold mb-3">
          🚧 BETA — We&apos;re still building! Expect rough edges and frequent updates.
        </span>
        <h1 className="text-2xl md:text-3xl font-bold text-[var(--sc-secondary)] mt-2">
          Predict. Compete. Prove You Know Football.
        </h1>
        <p className="mt-3 max-w-lg text-[var(--sc-text-secondary)]">
          Predict exact scorelines, compete with friends in private leagues, and spice things up with risk plays — all for free, no betting involved.
        </p>
        <Link href="/login" className="mt-5 px-6 py-3 rounded-lg bg-[var(--sc-tertiary)] text-white font-semibold">
          Get Started Free
        </Link>
        <div className="mt-6 max-w-md border border-[var(--sc-border)] bg-[var(--sc-surface)] rounded-xl p-3 text-center">
          <div className="text-xs font-bold text-[var(--sc-text-secondary)] tracking-wider">🚫 NOT A BETTING SITE</div>
          <div className="text-xs text-[var(--sc-text-secondary)] mt-1">
            ScoreCast is a free prediction game for fun among friends. We do not facilitate, encourage, or endorse gambling or betting of any kind.
          </div>
        </div>
      </section>

      {/* Features */}
      <section className="px-4">
        <h2 className="text-2xl font-bold text-center mb-6">What You Get</h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4">
          {features.map((f) => (
            <div key={f.title} className="bg-[var(--sc-surface)] rounded-xl p-4 shadow-sm">
              <div className={`w-10 h-10 rounded-full flex items-center justify-center text-white text-lg mb-3 ${colorClass(f.color)}`}>
                {f.icon}
              </div>
              <div className="font-semibold mb-1">{f.title}</div>
              <div className="text-sm text-[var(--sc-text-secondary)]">{f.desc}</div>
            </div>
          ))}
        </div>
      </section>

      {/* Competitions */}
      <section className="px-4">
        <h2 className="text-2xl font-bold text-center mb-1">Supported Competitions</h2>
        <p className="text-sm text-center text-[var(--sc-text-secondary)] mb-4">More coming soon</p>
        <div className="flex justify-center gap-10 flex-wrap">
          <div className="text-center">
            <div className="text-4xl mb-1">🛡️</div>
            <div className="font-semibold">Premier League</div>
            <div className="text-xs text-[var(--sc-text-secondary)]">38 matchweeks, full season</div>
          </div>
          <div className="text-center">
            <div className="text-4xl mb-1">🌍</div>
            <div className="font-semibold">FIFA World Cup 2026</div>
            <div className="text-xs text-[var(--sc-text-secondary)]">Groups + knockout bracket</div>
          </div>
        </div>
      </section>

      {/* How it works */}
      <section className="px-4">
        <h2 className="text-2xl font-bold text-center mb-6">How It Works</h2>
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 max-w-3xl mx-auto">
          {[
            { n: 1, t: "Sign Up", d: "Create your free account with email or Google sign-in." },
            { n: 2, t: "Predict", d: "Submit exact scorelines and add risk plays before kickoff." },
            { n: 3, t: "Dominate", d: "Earn points, climb the leaderboard, settle the debate." },
          ].map((s) => (
            <div key={s.n} className="flex flex-col items-center text-center">
              <div className="w-12 h-12 rounded-full bg-[var(--sc-secondary)] text-white font-bold text-xl flex items-center justify-center mb-2">{s.n}</div>
              <div className="font-semibold">{s.t}</div>
              <div className="text-sm text-[var(--sc-text-secondary)]">{s.d}</div>
            </div>
          ))}
        </div>
        <div className="flex justify-center mt-6">
          <Link href="/how-to-play" className="px-4 py-2 rounded-lg bg-[var(--sc-primary)] text-white text-sm font-semibold">
            Full rules & scoring guide →
          </Link>
        </div>
      </section>

      {/* FAQ */}
      <section className="px-4 max-w-2xl mx-auto">
        <h2 className="text-2xl font-bold text-center mb-6">FAQ</h2>
        <div className="space-y-2">
          {faqs.map((f, i) => (
            <div key={i} className="bg-[var(--sc-surface)] rounded-lg overflow-hidden">
              <button onClick={() => setOpenFaq(openFaq === i ? null : i)} className="w-full px-4 py-3 text-left font-semibold flex items-center justify-between">
                <span>{f.q}</span>
                <span className="text-[var(--sc-text-secondary)]">{openFaq === i ? "−" : "+"}</span>
              </button>
              {openFaq === i && <div className="px-4 pb-4 text-sm text-[var(--sc-text-secondary)]">{f.a}</div>}
            </div>
          ))}
        </div>
      </section>

      {/* Powered By */}
      <section className="px-4 max-w-2xl mx-auto">
        <h2 className="text-2xl font-bold text-center mb-1">Powered By</h2>
        <p className="text-sm text-center text-[var(--sc-text-secondary)] mb-6">ScoreCast wouldn&apos;t be possible without these amazing services and APIs.</p>
        <div className="grid grid-cols-2 sm:grid-cols-3 gap-4">
          {poweredBy.map((p) => (
            <div key={p.title} className="flex flex-col items-center text-center">
              <div className="text-3xl mb-1">{p.icon}</div>
              <div className="text-sm font-semibold">{p.title}</div>
              <div className="text-xs text-[var(--sc-text-secondary)]">{p.desc}</div>
            </div>
          ))}
        </div>
      </section>

      {/* Final CTA */}
      <section className="px-4 text-center pb-6">
        <h3 className="text-xl font-bold">Ready to prove you know football?</h3>
        <Link href="/login" className="inline-block mt-3 px-6 py-3 rounded-lg bg-[var(--sc-tertiary)] text-white font-semibold">
          🚀 Join the Beta
        </Link>
      </section>
    </div>
  );
}
