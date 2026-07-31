import type { Metadata } from 'next';
import { RegistryPage } from '@/components/registry/registry-page';
import { FormBuilder } from '@/components/registry/form-builder';

export const metadata: Metadata = { title: 'Forms' };

/**
 * The form builder, with the preview beside the schema.
 *
 * Every form in the product — the sell flow, dealer onboarding, the 200-point inspection checklist,
 * the loan application — is one of these documents. The preview renders with the *same* component
 * the customer app uses, so what an operator sees here is literally what ships, on web and on both
 * mobile apps, the moment they publish.
 */
export default function FormRegistryPage() {
  return (
    <RegistryPage
      eyebrow="Registry 2 of 10"
      title="Forms"
      description="Steps, fields, validation rules and conditional visibility. Published versions are immutable."
      version={7}
      publishedBy="product@autohub.in"
    >
      <FormBuilder />
    </RegistryPage>
  );
}
