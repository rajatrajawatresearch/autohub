'use client';

/**
 * Auth hooks.
 *
 * `useMe` is the permission source for the whole shell. Show and hide on `permissions`, never on
 * `roles` — a role is a grouping, and the server enforces on permissions regardless.
 */

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { getMe, logout, requestOtp, verifyOtp } from '../endpoints/auth';
import { getAuthTokens } from '../http';
import type { AuthSession, AuthUser, OtpChallenge, OtpRequest, OtpVerifyRequest } from '../schemas';
import { queryKeys } from './keys';

export function useMe(enabled = true) {
  const query = useQuery<AuthUser>({
    queryKey: queryKeys.me(),
    queryFn: () => getMe(),
    enabled,
    staleTime: 60_000,
    retry: false,
  });

  const can = (permission: string): boolean =>
    query.data?.permissions.includes(permission) ?? false;

  const canAny = (...permissions: string[]): boolean => permissions.some(can);

  return { ...query, can, canAny };
}

export function useRequestOtp() {
  return useMutation<OtpChallenge, Error, OtpRequest>({
    mutationFn: (request) => requestOtp(request),
  });
}

export function useVerifyOtp() {
  const queryClient = useQueryClient();
  return useMutation<AuthSession, Error, OtpVerifyRequest>({
    mutationFn: (request) => verifyOtp(request),
    onSuccess: (session) => {
      queryClient.setQueryData(queryKeys.me(), session.user);
    },
  });
}

export function useLogout() {
  const queryClient = useQueryClient();
  return useMutation<void, Error, void>({
    mutationFn: () => logout(getAuthTokens()?.refreshToken ?? ''),
    onSettled: () => {
      /* Everything cached was cached for somebody. Drop all of it. */
      queryClient.clear();
    },
  });
}
