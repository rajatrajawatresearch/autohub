'use client';

import { useRef, useState } from 'react';
import Link from 'next/link';
import { useFormDefinition, useCreateListingDraft, ApiError } from '@autohub/api';
import { SchemaForm, type FormValues, type SchemaFormApi } from '@autohub/forms';
import { Button, EmptyState, SectionHeading, Skeleton } from '@autohub/ui';

/**
 * Renders whatever the registry says the sell form is.
 *
 * The only product knowledge in this file is which form key to ask for and what to do with the
 * answer. Everything between those two points — steps, fields, validation, the EV-only battery
 * question — is data.
 */
export function SellFlow() {
  const definition = useFormDefinition('sell_vehicle');
  const createDraft = useCreateListingDraft();
  const formApi = useRef<SchemaFormApi | null>(null);
  const [submitted, setSubmitted] = useState<{ listingId: string } | null>(null);

  async function handleSubmit(values: FormValues) {
    try {
      const draft = await createDraft.mutateAsync({
        intent: 'sale',
        // The form itself carries the category the seller picked. Reading it out of the payload
        // rather than hardcoding 'car' is what lets the same flow list a truck or a scooter.
        categoryCode: String(values['category'] ?? 'car'),
        registrationNumber:
          typeof values['registration_number'] === 'string'
            ? values['registration_number']
            : undefined,
        values,
      });
      setSubmitted({ listingId: draft.listingId });
    } catch (error) {
      // A 422 carries the server's own field errors. Pushing them back into the form puts each
      // message under the field it belongs to, instead of one banner that says "check your input".
      if (error instanceof ApiError && error.fieldErrors) {
        formApi.current?.applyServerFieldErrors(error.fieldErrors);
      }
    }
  }

  if (submitted) {
    return (
      <div className="rounded-[var(--radius-card)] border border-[color:var(--border-hairline)] p-8">
        <SectionHeading
          eyebrow="Submitted"
          title="Your vehicle is with our team"
          description="We check the RC against VAHAN and run a photo review. You will hear from us within four hours."
          level={2}
        />
        <div className="mt-6 flex flex-wrap gap-3">
          <Button asChild>
            <Link href={`/buy/${submitted.listingId}`}>See your listing</Link>
          </Button>
          <Button variant="outline" asChild>
            <Link href="/">Value another vehicle</Link>
          </Button>
        </div>
      </div>
    );
  }

  if (definition.isLoading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-10 w-2/3" />
        <Skeleton className="h-[var(--density-control-h)] w-full" />
        <Skeleton className="h-[var(--density-control-h)] w-full" />
        <Skeleton className="h-[var(--density-control-h)] w-1/2" />
      </div>
    );
  }

  if (definition.isError || !definition.data) {
    return (
      <EmptyState
        title="The sell form did not load"
        body="The form registry is unreachable. Retry — nothing you entered has been lost."
        action={
          <Button variant="outline" onClick={() => definition.refetch()}>
            Retry
          </Button>
        }
      />
    );
  }

  return (
    <SchemaForm
      definition={definition.data}
      onSubmit={handleSubmit}
      apiRef={formApi}
      showTitle={false}
      error={
        createDraft.isError && !(createDraft.error instanceof ApiError && createDraft.error.fieldErrors)
          ? 'That did not save. Check your connection and submit again.'
          : null
      }
    />
  );
}
