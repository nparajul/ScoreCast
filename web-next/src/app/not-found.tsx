import Link from "next/link";

export default function NotFound() {
  return (
    <div className="flex flex-col items-center justify-center min-h-[80vh] text-white">
      <div className="text-6xl mb-4">⚽</div>
      <h1 className="text-2xl font-extrabold mb-2">Page not found</h1>
      <p className="text-sm opacity-60 mb-6">The page you&apos;re looking for doesn&apos;t exist.</p>
      <Link href="/dashboard" className="px-6 py-2 bg-[#FF6B35] rounded-full font-bold text-sm">
        Back to Dashboard
      </Link>
    </div>
  );
}
