import { useEffect, useRef, useState } from 'react';

/**
 * Marca un nodo como visible la primera vez que entra al viewport.
 * Sirve para fade/slide y para disparar contadores — una sola vez, sin
 * rebotar cada vez que el profe scrollea para arriba.
 */
export function useRevelar<T extends HTMLElement = HTMLDivElement>() {
  const ref = useRef<T>(null);
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const obs = new IntersectionObserver(
      ([entry]) => {
        if (!entry.isIntersecting) return;
        setVisible(true);
        obs.disconnect();
      },
      { threshold: 0.2, rootMargin: '0px 0px -8% 0px' },
    );
    obs.observe(el);
    return () => obs.disconnect();
  }, []);

  return { ref, visible };
}
