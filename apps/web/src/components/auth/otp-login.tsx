'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { useRequestOtp, useVerifyOtp } from '@autohub/api';
import { Button, Input, SectionHeading } from '@autohub/ui';

/**
 * Two screens, no password.
 *
 * India is phone-first: a password is a support ticket waiting to happen and one more thing for a
 * user to lose. The OTP is the primary factor, not a second one.
 */
export function OtpLogin() {
  const router = useRouter();
  const [phone, setPhone] = useState('');
  const [code, setCode] = useState('');
  const [challengeId, setChallengeId] = useState<string | null>(null);
  const [resendIn, setResendIn] = useState(0);

  const request = useRequestOtp();
  const verify = useVerifyOtp();

  useEffect(() => {
    if (resendIn <= 0) return;
    const timer = setTimeout(() => setResendIn((s) => s - 1), 1000);
    return () => clearTimeout(timer);
  }, [resendIn]);

  const digits = phone.replace(/\D/g, '');
  const canRequest = digits.length === 10 && !request.isPending;

  async function sendCode() {
    if (!canRequest) return;
    const challenge = await request.mutateAsync({ phone: `+91${digits}`, purpose: 'login' });
    setChallengeId(challenge.challengeId);
    setResendIn(challenge.resendAfterSeconds ?? 30);
  }

  async function submitCode() {
    if (!challengeId || code.length !== 6) return;
    await verify.mutateAsync({ challengeId, code });
    router.push('/');
  }

  return (
    <div className="w-full max-w-[24rem]">
      <SectionHeading
        eyebrow={challengeId ? 'Step 2 of 2' : 'Step 1 of 2'}
        title={challengeId ? 'Enter the code' : 'Sign in'}
        description={
          challengeId
            ? `We sent a 6-digit code to +91 ${digits}.`
            : 'We will text you a code. No password to remember.'
        }
        level={1}
      />

      {challengeId === null ? (
        <div className="mt-8 space-y-4">
          <label className="block">
            <span className="ah-label">Mobile number</span>
            <div className="mt-1 flex">
              <span className="inline-flex h-[var(--density-control-h)] items-center rounded-l-[var(--radius-control)] border border-r-0 border-[color:var(--border-hairline)] bg-[color:var(--surface-sunken)] px-3 font-mono text-data text-[color:var(--text-secondary)]">
                +91
              </span>
              <Input
                inputMode="numeric"
                autoComplete="tel-national"
                value={phone}
                onChange={(event) => setPhone(event.target.value.replace(/\D/g, '').slice(0, 10))}
                onKeyDown={(event) => event.key === 'Enter' && void sendCode()}
                placeholder="98765 43210"
                aria-label="Mobile number"
                className="rounded-l-none font-mono"
              />
            </div>
          </label>

          <Button size="lg" className="w-full" onClick={sendCode} disabled={!canRequest}>
            {request.isPending ? 'Sending…' : 'Send code'}
          </Button>

          {request.isError ? (
            <p className="text-body-sm text-[color:var(--color-flag)]">
              That number did not go through. Check the digits and try again.
            </p>
          ) : null}
        </div>
      ) : (
        <div className="mt-8 space-y-4">
          <label className="block">
            <span className="ah-label">6-digit code</span>
            <Input
              inputMode="numeric"
              autoComplete="one-time-code"
              autoFocus
              value={code}
              onChange={(event) => setCode(event.target.value.replace(/\D/g, '').slice(0, 6))}
              onKeyDown={(event) => event.key === 'Enter' && void submitCode()}
              placeholder="482915"
              aria-label="6-digit code"
              className="mt-1 font-mono tracking-[0.4em]"
            />
          </label>

          <Button
            size="lg"
            className="w-full"
            onClick={submitCode}
            disabled={code.length !== 6 || verify.isPending}
          >
            {verify.isPending ? 'Checking…' : 'Sign in'}
          </Button>

          {verify.isError ? (
            <p className="text-body-sm text-[color:var(--color-flag)]">
              That code did not match. Check it, or request a new one.
            </p>
          ) : null}

          <div className="flex items-center justify-between text-body-sm">
            <button
              type="button"
              onClick={() => {
                setChallengeId(null);
                setCode('');
              }}
              className="text-[color:var(--text-secondary)] underline underline-offset-4"
            >
              Change number
            </button>
            <button
              type="button"
              onClick={sendCode}
              disabled={resendIn > 0}
              className="text-[color:var(--accent-primary)] underline underline-offset-4 disabled:text-[color:var(--text-secondary)] disabled:no-underline"
            >
              {resendIn > 0 ? `Resend in ${resendIn}s` : 'Resend code'}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
