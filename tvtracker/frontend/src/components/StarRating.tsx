import { useState } from 'react';
import { Star } from 'lucide-react';

interface StarRatingProps {
  value?: number;
  max?: number;
  onChange?: (rating: number) => void;
  size?: number;
  readonly?: boolean;
}

export default function StarRating({ value = 0, max = 10, onChange, size = 18, readonly = false }: StarRatingProps) {
  const [hovered, setHovered] = useState(0);
  const display = hovered || value;
  const stars = max === 10 ? 5 : max; // Show 5 stars for 10-point scale

  return (
    <div style={{ display: 'flex', gap: 2, alignItems: 'center' }}>
      {Array.from({ length: stars }, (_, i) => {
        const starValue = max === 10 ? (i + 1) * 2 : i + 1;
        const filled = display >= starValue;
        const halfFilled = max === 10 && display >= starValue - 1 && !filled;
        return (
          <button
            key={i}
            onClick={() => !readonly && onChange?.(starValue)}
            onMouseEnter={() => !readonly && setHovered(starValue)}
            onMouseLeave={() => !readonly && setHovered(0)}
            style={{
              background: 'none', cursor: readonly ? 'default' : 'pointer',
              padding: 1, border: 'none', display: 'flex',
            }}
            disabled={readonly}
          >
            <Star
              size={size}
              fill={filled ? 'var(--gold)' : halfFilled ? 'var(--gold)' : 'none'}
              color={filled || halfFilled ? 'var(--gold)' : 'var(--border-light)'}
              style={{ transition: 'all 0.1s' }}
            />
          </button>
        );
      })}
      {value > 0 && (
        <span style={{ fontSize: 13, color: 'var(--text-secondary)', marginLeft: 4, fontWeight: 500 }}>
          {value}/{max}
        </span>
      )}
    </div>
  );
}
