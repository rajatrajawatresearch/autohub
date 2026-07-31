import type { Metadata } from 'next';
import { Badge, Button, SectionHeading } from '@autohub/ui';

export const metadata: Metadata = { title: 'Integrations' };

/**
 * Provider routing per capability, with a fallback chain.
 *
 * This is what makes "swap Razorpay for Cashfree" a config change: code depends on the capability
 * `payment.collect`, never on a vendor. Credentials are never stored here — a provider row holds a
 * reference into the secret store, so a database dump is not a set of live API keys.
 */
export default function IntegrationsPage() {
  return (
    <div className="mx-auto max-w-[var(--container-wide)] px-4 py-6">
      <SectionHeading
        eyebrow="Registry 8 of 10"
        title="Integrations"
        description="Capabilities, providers and the order they are tried in. Credentials live in the secret store, never here."
        level={1}
      />

      <div className="mt-6 space-y-4">
        {CAPABILITIES.map((capability) => (
          <section key={capability.code} className="rounded-[var(--radius-card)] border border-[color:var(--border-hairline)] bg-[color:var(--surface-card)] p-4">
            <div className="flex flex-wrap items-baseline gap-3">
              <span className="font-mono text-body-sm">{capability.code}</span>
              <span className="text-body-sm text-[color:var(--text-secondary)]">{capability.label}</span>
              <Button variant="outline" size="sm" className="ml-auto">Test call</Button>
            </div>

            <ol className="mt-3 divide-y divide-[color:var(--border-hairline)] border-t border-[color:var(--border-hairline)]">
              {capability.providers.map((provider, index) => (
                <li key={provider.name} className="flex flex-wrap items-center gap-3 py-2">
                  <span className="ah-label w-16">{index === 0 ? 'Primary' : `Fallback ${index}`}</span>
                  <span className="text-body-sm">{provider.name}</span>
                  <Badge variant={provider.health === 'healthy' ? 'clear' : provider.health === 'degraded' ? 'caution' : 'flag'} size="sm">
                    {provider.health}
                  </Badge>
                  <span className="ml-auto font-mono text-[0.6875rem] text-[color:var(--text-secondary)]">
                    {provider.credentialRef}
                  </span>
                </li>
              ))}
            </ol>
          </section>
        ))}
      </div>
    </div>
  );
}

const CAPABILITIES = [
  { code: 'payment.collect', label: 'Collect a payment', providers: [
    { name: 'Development gateway', health: 'healthy', credentialRef: 'secret://dev/payment' },
    { name: 'Razorpay', health: 'healthy', credentialRef: 'secret://prod/razorpay' },
  ]},
  { code: 'sms.send', label: 'Transactional SMS (DLT registered)', providers: [
    { name: 'Console writer', health: 'healthy', credentialRef: 'secret://dev/sms' },
  ]},
  { code: 'whatsapp.send', label: 'WhatsApp Business message', providers: [
    { name: 'Console writer', health: 'healthy', credentialRef: 'secret://dev/whatsapp' },
  ]},
  { code: 'kyc.pan_verify', label: 'PAN verification', providers: [
    { name: 'Stub verifier', health: 'healthy', credentialRef: 'secret://dev/kyc' },
  ]},
  { code: 'ocr.rc', label: 'RC document OCR', providers: [
    { name: 'Stub OCR', health: 'degraded', credentialRef: 'secret://dev/ocr' },
  ]},
  { code: 'valuation.predict', label: 'Valuation model', providers: [
    { name: 'Hedonic rule-based v1', health: 'healthy', credentialRef: 'n/a — in-process' },
    { name: 'ML ensemble (shadow)', health: 'down', credentialRef: 'secret://prod/valuation' },
  ]},
] as const;
