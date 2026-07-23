export default function Footer() {
  return (
    <footer className="bg-bg-card border-t border-gray-200 mt-auto">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6 flex flex-col md:flex-row justify-between items-center gap-4">
        <p className="text-text-muted text-sm text-center md:text-left">
          © {new Date().getFullYear()} TaskTracker. Всі права захищені.
        </p>
        {/* <div className="flex gap-4 text-sm">
          <a href="#" className="text-text-muted hover:text-primary transition">Документація</a>
          <a href="#" className="text-text-muted hover:text-primary transition">Підтримка</a>
        </div> */}
      </div>
    </footer>
  );
}