"use client";

export function ScoreCastLoading({ size = "text-4xl", message }: { size?: string; message?: string }) {
  return (
    <div className="flex flex-col items-center justify-center gap-2">
      <div className={`${size} animate-pulse`}>⚽</div>
      {message && <p className="text-sm text-[var(--sc-text-secondary)]">{message}</p>}
    </div>
  );
}
