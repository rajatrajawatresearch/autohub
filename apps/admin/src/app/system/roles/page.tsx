import type { Metadata } from 'next';
import { Badge, SectionHeading } from '@autohub/ui';

export const metadata: Metadata = { title: 'Roles & permissions' };

/**
 * The registry that kills `if (user.Role == "Dealer")`.
 *
 * Code checks a permission; a role is only a bundle of them. The column that does the real work is
 * Scope: "Regional Auction Moderator — West" is an existing role assigned over a set of cities, not
 * a new role and certainly not a new branch in the codebase.
 */
export default function RolesPage() {
  return (
    <div className="mx-auto max-w-[var(--container-wide)] px-4 py-6">
      <SectionHeading
        eyebrow="System"
        title="Roles & permissions"
        description="Permissions are checked; roles are a grouping. Scope is what makes a regional role possible without a new one."
        level={1}
      />

      <SectionHeading title="Roles" level={2} size="sm" rule className="mt-6" />
      <div className="mt-4 overflow-x-auto">
        <table className="w-full border-collapse text-body-sm">
          <thead>
            <tr className="border-b border-[color:var(--border-hairline)]">
              <th className="ah-label py-2 text-left">Role</th>
              <th className="ah-label py-2 text-left">Scope type</th>
              <th className="ah-label py-2 text-left">Permissions</th>
              <th className="ah-label py-2 text-left">System</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-[color:var(--border-hairline)]">
            {ROLES.map((role) => (
              <tr key={role.code}>
                <td className="py-2 pr-3"><span className="font-mono">{role.code}</span><span className="block text-[color:var(--text-secondary)]">{role.label}</span></td>
                <td className="py-2 pr-3 font-mono text-[0.75rem]">{role.scope}</td>
                <td className="py-2 pr-3 font-mono text-[0.75rem]">{role.permissions}</td>
                <td className="py-2">{role.system ? <Badge variant="neutral" size="sm">system</Badge> : null}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <SectionHeading
        title="Permission catalogue"
        description="Every code the codebase actually checks, so the matrix is complete rather than whatever happens to be in the table."
        level={2}
        size="sm"
        rule
        className="mt-10"
      />
      <div className="mt-4 grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
        {PERMISSION_GROUPS.map((group) => (
          <div key={group.module}>
            <p className="ah-label">{group.module}</p>
            <ul className="mt-2 space-y-1">
              {group.codes.map((code) => (
                <li key={code} className="flex items-center gap-2 font-mono text-[0.75rem]">
                  {code}
                  {code.includes('publish') || code.includes('payout') ? (
                    <Badge variant="flag" size="sm">dangerous</Badge>
                  ) : null}
                </li>
              ))}
            </ul>
          </div>
        ))}
      </div>
    </div>
  );
}

const ROLES = [
  { code: 'customer', label: 'Customer', scope: 'global', permissions: 9, system: true },
  { code: 'dealer_owner', label: 'Dealer owner', scope: 'dealer', permissions: 31, system: true },
  { code: 'dealer_staff', label: 'Dealer staff', scope: 'branch', permissions: 18, system: false },
  { code: 'fleet_owner', label: 'Fleet owner', scope: 'dealer', permissions: 14, system: false },
  { code: 'broker', label: 'Broker', scope: 'city', permissions: 11, system: false },
  { code: 'inspector', label: 'Inspector', scope: 'city', permissions: 8, system: true },
  { code: 'support_exec', label: 'Support executive', scope: 'global', permissions: 21, system: true },
  { code: 'auction_moderator', label: 'Auction moderator', scope: 'city', permissions: 16, system: true },
  { code: 'admin', label: 'Admin', scope: 'global', permissions: 64, system: true },
  { code: 'super_admin', label: 'Super admin', scope: 'global', permissions: 88, system: true },
] as const;

const PERMISSION_GROUPS = [
  { module: 'listing', codes: ['listing.create', 'listing.submit', 'listing.moderate', 'listing.delist'] },
  { module: 'auction', codes: ['auction.view', 'auction.bid.place', 'auction.lot.create', 'auction.lot.close'] },
  { module: 'deal', codes: ['deal.view', 'deal.accept', 'deal.cancel', 'deal.settle'] },
  { module: 'payment', codes: ['payment.view', 'payment.refund', 'payment.payout.release'] },
  { module: 'registry', codes: ['registry.taxonomy.publish', 'registry.form.publish', 'registry.workflow.publish', 'registry.pricing.publish'] },
  { module: 'system', codes: ['system.role.assign', 'system.audit.read', 'system.integration.configure'] },
] as const;
