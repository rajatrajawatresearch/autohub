import Link from 'next/link';

const COLUMNS = [
  {
    heading: 'Buy',
    links: [
      { href: '/buy', label: 'All vehicles' },
      { href: '/buy?intent=sale&attr.fuel_type=ev', label: 'Electric' },
      { href: '/rent', label: 'Rent' },
      { href: '/lease', label: 'Lease' },
    ],
  },
  {
    heading: 'Sell',
    links: [
      { href: '/sell', label: 'Sell your vehicle' },
      { href: '/', label: 'Free valuation' },
      { href: '/attach', label: 'Attach a taxi' },
    ],
  },
  {
    heading: 'Company',
    links: [
      { href: '/', label: 'About' },
      { href: '/', label: 'Careers' },
      { href: '/', label: 'Contact' },
    ],
  },
] as const;

export function SiteFooter() {
  return (
    <footer className="border-t border-[color:var(--border-hairline)]">
      <div className="mx-auto max-w-[var(--container-content)] px-4 py-12 sm:px-6">
        <div className="grid gap-10 sm:grid-cols-2 lg:grid-cols-4">
          <div>
            <p className="font-display text-[1.0625rem] font-bold tracking-[-0.02em]">
              AUTO<span className="text-[color:var(--accent-primary)]">HUB</span>
            </p>
            <p className="mt-3 max-w-[26ch] text-body-sm text-[color:var(--text-secondary)]">
              Buy, sell, rent, lease and auction vehicles across India.
            </p>
          </div>

          {COLUMNS.map((column) => (
            <div key={column.heading}>
              <p className="ah-label">{column.heading}</p>
              <ul className="mt-3 space-y-2">
                {column.links.map((link) => (
                  <li key={`${column.heading}-${link.label}`}>
                    <Link
                      href={link.href}
                      className="text-body-sm text-[color:var(--text-secondary)] transition-colors hover:text-[color:var(--text-primary)]"
                    >
                      {link.label}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        <div className="mt-12 flex flex-col gap-2 border-t border-[color:var(--border-hairline)] pt-6 text-body-sm text-[color:var(--text-secondary)] sm:flex-row sm:items-center sm:justify-between">
          <p>© {new Date().getFullYear()} AutoHub</p>
          <p className="font-mono text-[0.75rem]">
            Vehicle data sourced from VAHAN · Valuations are estimates, not offers
          </p>
        </div>
      </div>
    </footer>
  );
}
