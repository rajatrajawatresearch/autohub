'use client';

import { useState } from 'react';
import Image from 'next/image';
import type { MediaAsset } from '@autohub/api';
import { cn } from '@autohub/ui';

/**
 * Vehicle photography is the heaviest thing on this page, and a listing whose images reflow as
 * they load is a listing people bounce off. Every slot therefore has a known aspect ratio before
 * a byte arrives, and the cover is the only priority image.
 */
export function Gallery({
  assets,
  cover,
  aspectRatio = 1.5,
  alt,
}: {
  assets: MediaAsset[];
  cover?: string | null;
  aspectRatio?: number;
  alt: string;
}) {
  const images = assets.length > 0 ? assets : cover ? [{ id: 'cover', url: cover, aspectRatio }] : [];
  const [activeIndex, setActiveIndex] = useState(0);
  const active = images[activeIndex] ?? images[0];

  if (!active) {
    return (
      <div
        className="grid place-items-center rounded-[var(--radius-card)] border border-[color:var(--border-hairline)] bg-[color:var(--surface-sunken)]"
        style={{ aspectRatio }}
      >
        <p className="text-body-sm text-[color:var(--text-secondary)]">No photos yet</p>
      </div>
    );
  }

  return (
    <div>
      <div
        className="relative overflow-hidden rounded-[var(--radius-card)] border border-[color:var(--border-hairline)] bg-[color:var(--surface-sunken)]"
        style={{ aspectRatio: active.aspectRatio ?? aspectRatio }}
      >
        <Image
          src={active.url}
          alt={`${alt} — photo ${activeIndex + 1} of ${images.length}`}
          fill
          sizes="(max-width: 1024px) 100vw, 46rem"
          className="object-cover"
          priority={activeIndex === 0}
          unoptimized={active.url.startsWith('data:')}
        />
      </div>

      {images.length > 1 ? (
        <ul className="mt-3 flex gap-2 overflow-x-auto pb-1">
          {images.map((asset, index) => (
            <li key={asset.id}>
              <button
                type="button"
                onClick={() => setActiveIndex(index)}
                aria-label={`Show photo ${index + 1}`}
                aria-current={index === activeIndex}
                className={cn(
                  'relative h-16 w-24 shrink-0 overflow-hidden rounded-[var(--radius-control)] border transition-colors',
                  index === activeIndex
                    ? 'border-[color:var(--accent-primary)]'
                    : 'border-[color:var(--border-hairline)] hover:border-[color:var(--border-strong)]',
                )}
              >
                <Image
                  src={asset.url}
                  alt=""
                  fill
                  sizes="6rem"
                  className="object-cover"
                  unoptimized={asset.url.startsWith('data:')}
                />
              </button>
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}
