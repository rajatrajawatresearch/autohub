'use client';

import { useCallback, useEffect, useState } from 'react';

const STORAGE_KEY = 'autohub.city';
const COOKIE_KEY = 'autohub_city';
const DEFAULT_CITY = process.env.NEXT_PUBLIC_DEFAULT_CITY ?? 'pune';

/**
 * The active city, held in a cookie rather than only in React state.
 *
 * Search results, pricing and feature evaluation are all scoped by city, and Server Components read
 * it from the cookie on the next request. Keeping it in a client store alone would mean a
 * server-rendered page and the header disagreeing about where the user is.
 */
export function useActiveCity(): [string, (city: string) => void] {
  const [city, setCityState] = useState(DEFAULT_CITY);

  useEffect(() => {
    const stored = window.localStorage.getItem(STORAGE_KEY);
    if (stored) setCityState(stored);
  }, []);

  const setCity = useCallback((next: string) => {
    setCityState(next);
    window.localStorage.setItem(STORAGE_KEY, next);
    // Lax is right here: the city is a display preference, and a cross-site POST carrying it
    // changes nothing a user would mind.
    document.cookie = `${COOKIE_KEY}=${encodeURIComponent(next)}; path=/; max-age=31536000; samesite=lax`;
  }, []);

  return [city, setCity];
}
