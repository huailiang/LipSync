/* finds the complex roots of a polynomial */

#include <stdio.h>
#include <math.h>
#include "gsl_zsolve.h"

/* C-style matrix elements */
#define MAT(m, i, j, n) ((m)[(i) * (n) + (j)])

/* Fortran-style matrix elements */
#define FMAT(m, i, j, n) ((m)[((i)-1) * (n) + ((j)-1)])

#define RADIX 2
#define RADIX2 (RADIX * RADIX)

static int qr_companion(double *h, size_t nc, gsl_complex_packed_ptr zroot)
{
  double t = 0.0;
  size_t iterations, e, i, j, k, m;
  double w, x, y, s, z;
  double p = 0, q = 0, r = 0;

  /* FIXME: if p,q,r, are not set to zero then the compiler complains
     that they ``might be used uninitialized in this
     function''. Looking at the code this does seem possible, so this
     should be checked. */

  int notlast;

  size_t n = nc;

next_root:
  if (n == 0)
    return 1;

  iterations = 0;

next_iteration:

  for (e = n; e >= 2; e--)
  {
    double a1 = fabs(FMAT(h, e, e - 1, nc));
    double a2 = fabs(FMAT(h, e - 1, e - 1, nc));
    double a3 = fabs(FMAT(h, e, e, nc));

    if (a1 <= GSL_DBL_EPSILON * (a2 + a3))
      break;
  }

  x = FMAT(h, n, n, nc);
  if (e == n)
  {
    GSL_SET_COMPLEX_PACKED(zroot, n - 1, x + t, 0); /* one real root */
    n--;
    goto next_root;
  }
  y = FMAT(h, n - 1, n - 1, nc);
  w = FMAT(h, n - 1, n, nc) * FMAT(h, n, n - 1, nc);
  if (e == n - 1)
  {
    p = (y - x) / 2;
    q = p * p + w;
    y = sqrt(fabs(q));
    x += t;
    if (q > 0) /* two real roots */
    {
      if (p < 0)
        y = -y;
      y += p;
      GSL_SET_COMPLEX_PACKED(zroot, n - 1, x - w / y, 0);
      GSL_SET_COMPLEX_PACKED(zroot, n - 2, x + y, 0);
    }
    else
    {
      GSL_SET_COMPLEX_PACKED(zroot, n - 1, x + p, -y);
      GSL_SET_COMPLEX_PACKED(zroot, n - 2, x + p, y);
    }
    n -= 2;
    goto next_root;
  }

  /* No more roots found yet, do another iteration */
  if (iterations == 60) /* increased from 30 to 60 */
  {
    /* too many iterations - give up! */
    return 0;
  }

  if (iterations % 10 == 0 && iterations > 0)
  {
    /* use an exceptional shift */
    t += x;
    for (i = 1; i <= n; i++)
    {
      FMAT(h, i, i, nc) -= x;
    }
    s = fabs(FMAT(h, n, n - 1, nc)) + fabs(FMAT(h, n - 1, n - 2, nc));
    y = 0.75 * s;
    x = y;
    w = -0.4375 * s * s;
  }

  iterations++;
  for (m = n - 2; m >= e; m--)
  {
    double a1, a2, a3;
    z = FMAT(h, m, m, nc);
    r = x - z;
    s = y - z;
    p = FMAT(h, m, m + 1, nc) + (r * s - w) / FMAT(h, m + 1, m, nc);
    q = FMAT(h, m + 1, m + 1, nc) - z - r - s;
    r = FMAT(h, m + 2, m + 1, nc);
    s = fabs(p) + fabs(q) + fabs(r);
    p /= s;
    q /= s;
    r /= s;
    if (m == e)
      break;

    a1 = fabs(FMAT(h, m, m - 1, nc));
    a2 = fabs(FMAT(h, m - 1, m - 1, nc));
    a3 = fabs(FMAT(h, m + 1, m + 1, nc));
    if (a1 * (fabs(q) + fabs(r)) <= GSL_DBL_EPSILON * fabs(p) * (a2 + a3))
      break;
  }

  for (i = m + 2; i <= n; i++)
  {
    FMAT(h, i, i - 2, nc) = 0;
  }
  for (i = m + 3; i <= n; i++)
  {
    FMAT(h, i, i - 3, nc) = 0;
  }
  /* double QR step */
  for (k = m; k <= n - 1; k++)
  {
    notlast = (k != n - 1);
    if (k != m)
    {
      p = FMAT(h, k, k - 1, nc);
      q = FMAT(h, k + 1, k - 1, nc);
      r = notlast ? FMAT(h, k + 2, k - 1, nc) : 0.0;

      x = fabs(p) + fabs(q) + fabs(r);

      if (x == 0)
        continue; /* FIXME????? */
      p /= x;
      q /= x;
      r /= x;
    }

    s = sqrt(p * p + q * q + r * r);
    if (p < 0)
      s = -s;
    if (k != m)
    {
      FMAT(h, k, k - 1, nc) = -s * x;
    }
    else if (e != m)
    {
      FMAT(h, k, k - 1, nc) *= -1;
    }

    p += s;
    x = p / s;
    y = q / s;
    z = r / s;
    q /= p;
    r /= p;

    /* do row modifications */
    for (j = k; j <= n; j++)
    {
      p = FMAT(h, k, j, nc) + q * FMAT(h, k + 1, j, nc);
      if (notlast)
      {
        p += r * FMAT(h, k + 2, j, nc);
        FMAT(h, k + 2, j, nc) -= p * z;
      }
      FMAT(h, k + 1, j, nc) -= p * y;
      FMAT(h, k, j, nc) -= p * x;
    }
    j = (k + 3 < n) ? (k + 3) : n;

    /* do column modifications */
    for (i = e; i <= j; i++)
    {
      p = x * FMAT(h, i, k, nc) + y * FMAT(h, i, k + 1, nc);
      if (notlast)
      {
        p += z * FMAT(h, i, k + 2, nc);
        FMAT(h, i, k + 2, nc) -= p * r;
      }
      FMAT(h, i, k + 1, nc) -= p * q;
      FMAT(h, i, k, nc) -= p;
    }
  }
  goto next_iteration;
}

static void balance_companion_matrix(double *m, size_t nc)
{
  int not_converged = 1;
  double row_norm = 0;
  double col_norm = 0;
  while (not_converged)
  {
    size_t i, j;
    double g, f, s;
    not_converged = 0;
    for (i = 0; i < nc; i++)
    {
      /* column norm, excluding the diagonal */
      if (i != nc - 1)
      {
        col_norm = fabs(MAT(m, i + 1, i, nc));
      }
      else
      {
        col_norm = 0;
        for (j = 0; j < nc - 1; j++)
        {
          col_norm += fabs(MAT(m, j, nc - 1, nc));
        }
      }

      /* row norm, excluding the diagonal */
      if (i == 0)
      {
        row_norm = fabs(MAT(m, 0, nc - 1, nc));
      }
      else if (i == nc - 1)
      {
        row_norm = fabs(MAT(m, i, i - 1, nc));
      }
      else
      {
        row_norm = (fabs(MAT(m, i, i - 1, nc)) + fabs(MAT(m, i, nc - 1, nc)));
      }

      if (col_norm == 0 || row_norm == 0)
      {
        continue;
      }
      g = row_norm / RADIX;
      f = 1;
      s = col_norm + row_norm;
      while (col_norm < g)
      {
        f *= RADIX;
        col_norm *= RADIX2;
      }

      g = row_norm * RADIX;
      while (col_norm > g)
      {
        f /= RADIX;
        col_norm /= RADIX2;
      }

      if ((row_norm + col_norm) < 0.95 * s * f)
      {
        not_converged = 1;
        g = 1 / f;
        if (i == 0)
        {
          MAT(m, 0, nc - 1, nc) *= g;
        }
        else
        {
          MAT(m, i, i - 1, nc) *= g;
          MAT(m, i, nc - 1, nc) *= g;
        }

        if (i == nc - 1)
        {
          for (j = 0; j < nc; j++)
          {
            MAT(m, j, i, nc) *= f;
          }
        }
        else
        {
          MAT(m, i + 1, i, nc) *= f;
        }
      }
    }
  }
}

static void set_companion_matrix(const double *a, size_t nc, double *m)
{
  size_t i, j;
  for (i = 0; i < nc; i++)
    for (j = 0; j < nc; j++)
      MAT(m, i, j, nc) = 0.0;

  for (i = 1; i < nc; i++)
    MAT(m, i, i - 1, nc) = 1.0;

  for (i = 0; i < nc; i++)
    MAT(m, i, nc - 1, nc) = -a[i] / a[nc];
}

gsl_poly_complex_workspace *
gsl_poly_complex_workspace_alloc(size_t n)
{
  size_t nc;
  gsl_poly_complex_workspace *w;
  if (n == 0)
  {
    printf("matrix size n must be positive integer");
  }
  w = (gsl_poly_complex_workspace *)malloc(sizeof(gsl_poly_complex_workspace));
  if (w == 0)
  {
    printf("failed to allocate space for struct");
  }

  nc = n - 1;
  w->nc = nc;
  w->matrix = (double *)malloc(nc * nc * sizeof(double));
  if (w->matrix == 0)
  {
    free(w); /* error in constructor, avoid memory leak */
    printf("failed to allocate space for workspace matrix");
  }
  return w;
}

void gsl_poly_complex_workspace_free(gsl_poly_complex_workspace *w)
{
  if (w == NULL)
    return;
  free(w->matrix);
  free(w);
}

int gsl_poly_complex_solve(const double *a, size_t n, gsl_poly_complex_workspace *w, gsl_complex_packed_ptr z)
{
  int status;
  double *m;
  if (n == 0)
  {
    printf("number of terms must be a positive integeri\n");
  }
  if (n == 1)
  {
    printf("cannot solve for only one term\n");
  }
  if (a[n - 1] == 0)
  {
    printf("leading term of polynomial must be non-zero\n");
  }
  if (w->nc != n - 1)
  {
    printf("size of workspace does not match polynomial\n");
  }

  m = w->matrix;
  set_companion_matrix(a, n - 1, m);
  balance_companion_matrix(m, n - 1);
  status = qr_companion(m, n - 1, z);
  if (!status)
  {
    printf("root solving qr method failed to converge\n");
    return 0;
  }
  return 1;
}

C_EXPORT void poly_roots(int size, double* c, double* z)
{
    gsl_poly_complex_workspace *w = gsl_poly_complex_workspace_alloc(size);
    gsl_poly_complex_solve(c, size, w, z);
    gsl_poly_complex_workspace_free(w);
}


int main()
{
  int i;
  double a[8] = {-12, 19, 24, 20, 18, 41,87, 23};
  double z[14];
  poly_roots(8, a, z);
  for (i = 0; i < 7; i++)
  {
    printf("z%d = %+.8f, %+.8f\n", i, z[2 * i], z[2 * i + 1]);
  }
  return 0;
}
