export default function HowToPlayPage() {
  const rules = [
    { outcome: "Exact Scoreline", pts: 10, example: "You predicted 2-1, result was 2-1", bg: "rgba(46,125,50,0.15)" },
    { outcome: "Correct Result + GD", pts: 7, example: "You predicted 3-2, result was 2-1", bg: "rgba(0,131,143,0.15)" },
    { outcome: "Correct Result", pts: 5, example: "You predicted 3-0, result was 2-1", bg: "rgba(255,179,0,0.15)" },
    { outcome: "Correct Goal Difference", pts: 3, example: "You predicted 2-1, result was 0-1 (both GD of 1)", bg: "rgba(156,39,176,0.12)" },
    { outcome: "Incorrect", pts: 0, example: "Nothing matched", bg: "rgba(211,47,47,0.12)" },
  ];

  return (
    <div className="max-w-2xl mx-auto px-4 py-6 text-white pb-24">
      <h1 className="text-3xl font-extrabold text-center mb-1">How to Play</h1>
      <p className="text-center text-sm opacity-60 mb-8">Everything you need to know about ScoreCast</p>

      <h2 className="text-xl font-bold mb-2">🎯 The Basics</h2>
      <p className="mb-1">ScoreCast is a free football prediction game. Before each matchweek, you predict the exact scoreline for every match. After the matches are played, you earn points based on how close you were.</p>
      <p className="text-sm opacity-60 mb-6">Predictions lock at kickoff — once a match starts, you can&apos;t change your prediction for that match.</p>

      <h2 className="text-xl font-bold mb-2">📊 Scoring</h2>
      <p className="text-sm opacity-60 mb-3">Points are awarded based on how accurate your prediction is:</p>
      <div className="rounded-xl overflow-hidden border border-white/10 mb-2">
        <table className="w-full text-sm">
          <thead><tr className="border-b border-white/10"><th className="p-3 text-left">Outcome</th><th className="p-3 text-center">Pts</th><th className="p-3 text-left">Example</th></tr></thead>
          <tbody>
            {rules.map(r => (
              <tr key={r.outcome} style={{ background: r.bg }}>
                <td className="p-3 font-semibold">{r.outcome}</td>
                <td className="p-3 text-center text-lg font-extrabold">{r.pts}</td>
                <td className="p-3 text-xs opacity-70">{r.example}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <p className="text-xs opacity-50 mb-6">With 10 matches per gameweek, the maximum prediction score is 100 points.</p>

      <h2 className="text-xl font-bold mb-2">👥 Private Leagues</h2>
      <p className="mb-1">Create a league and share the 6-character invite code with your friends. Everyone in the league competes on the same leaderboard.</p>
      <p className="text-sm opacity-60 mb-6">Leagues are tied to a specific competition and season. You can join multiple leagues.</p>

      <h2 className="text-xl font-bold mb-2">🔒 Prediction Visibility</h2>
      <p className="mb-1">You can&apos;t see another player&apos;s predictions until the match kicks off. This prevents copying.</p>
      <p className="text-sm opacity-60 mb-6">Risk plays are hidden until all matches in the gameweek have kicked off.</p>

      <h2 className="text-xl font-bold mb-2">⚡ Live Scoring</h2>
      <p className="mb-1">During matches, your points update in real time. Live previews are provisional and can change (e.g. VAR).</p>
      <p className="text-sm opacity-60 mb-6">Points are only finalized when a match reaches full time.</p>

      <div className="border-2 border-yellow-500 rounded-xl p-4">
        <p className="font-bold">🚫 This is NOT a betting site</p>
        <p className="text-sm opacity-70 mt-1">ScoreCast is a free prediction game played purely for fun and bragging rights. There is no real money involved.</p>
      </div>
    </div>
  );
}
