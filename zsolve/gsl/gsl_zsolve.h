#ifndef __GSL_POLY_H__
#define __GSL_POLY_H__

#include <stdlib.h>

#undef __BEGIN_DECLS
#undef __END_DECLS

#ifdef __cplusplus
# define __BEGIN_DECLS extern "C" {
# define __END_DECLS }
#else
# define __BEGIN_DECLS /* empty */
# define __END_DECLS /* empty */
#endif

__BEGIN_DECLS

#define GSL_DBL_EPSILON        2.2204460492503131e-16

/* two consecutive built-in types as a complex number */
typedef double *       gsl_complex_packed ;
typedef float *        gsl_complex_packed_float  ;
typedef long double *  gsl_complex_packed_long_double ;

typedef const double *       gsl_const_complex_packed ;
typedef const float *        gsl_const_complex_packed_float  ;
typedef const long double *  gsl_const_complex_packed_long_double ;


/* 2N consecutive built-in types as N complex numbers */
typedef double *       gsl_complex_packed_array ;
typedef float *        gsl_complex_packed_array_float  ;
typedef long double *  gsl_complex_packed_array_long_double ;

typedef const double *       gsl_const_complex_packed_array ;
typedef const float *        gsl_const_complex_packed_array_float  ;
typedef const long double *  gsl_const_complex_packed_array_long_double ;



/* Yes... this seems weird. Trust us. The point is just that
   sometimes you want to make it obvious that something is
   an output value. The fact that it lacks a 'const' may not
   be enough of a clue for people in some contexts.
 */
typedef double *       gsl_complex_packed_ptr ;
typedef float *        gsl_complex_packed_float_ptr  ;
typedef long double *  gsl_complex_packed_long_double_ptr ;

typedef const double *       gsl_const_complex_packed_ptr ;
typedef const float *        gsl_const_complex_packed_float_ptr  ;
typedef const long double *  gsl_const_complex_packed_long_double_ptr ;


typedef struct
  {
    long double dat[2];
  }
gsl_complex_long_double;

typedef struct
  {
    double dat[2];
  }
gsl_complex;

typedef struct
  {
    float dat[2];
  }
gsl_complex_float;

#define GSL_REAL(z)     ((z).dat[0])
#define GSL_IMAG(z)     ((z).dat[1])
#define GSL_COMPLEX_P(zp) ((zp)->dat)
#define GSL_COMPLEX_P_REAL(zp)  ((zp)->dat[0])
#define GSL_COMPLEX_P_IMAG(zp)  ((zp)->dat[1])
#define GSL_COMPLEX_EQ(z1,z2) (((z1).dat[0] == (z2).dat[0]) && ((z1).dat[1] == (z2).dat[1]))

#define GSL_SET_COMPLEX(zp,x,y) do {(zp)->dat[0]=(x); (zp)->dat[1]=(y);} while(0)
#define GSL_SET_REAL(zp,x) do {(zp)->dat[0]=(x);} while(0)
#define GSL_SET_IMAG(zp,y) do {(zp)->dat[1]=(y);} while(0)

#define GSL_SET_COMPLEX_PACKED(zp,n,x,y) do {*((zp)+2*(n))=(x); *((zp)+(2*(n)+1))=(y);} while(0)


/* Evaluate polynomial
 *
 * c[0] + c[1] x + c[2] x^2 + ... + c[len-1] x^(len-1)
 *
 * exceptions: none
 */

__inline double gsl_poly_eval(const double c[], const int len, const double x)
{
  int i;
  double ans = c[len-1];
  for(i=len-1; i>0; i--) ans = c[i-1] + x * ans;
  return ans;
}

__inline gsl_complex gsl_poly_complex_eval(const double c[], const int len, const gsl_complex z)
{
  int i;
  gsl_complex ans;
  GSL_SET_COMPLEX (&ans, c[len-1], 0.0);
  for(i=len-1; i>0; i--) {
    /* The following three lines are equivalent to
       ans = gsl_complex_add_real (gsl_complex_mul (z, ans), c[i-1]); 
       but faster */
    double tmp = c[i-1] + GSL_REAL (z) * GSL_REAL (ans) - GSL_IMAG (z) * GSL_IMAG (ans);
    GSL_SET_IMAG (&ans, GSL_IMAG (z) * GSL_REAL (ans) + GSL_REAL (z) * GSL_IMAG (ans));
    GSL_SET_REAL (&ans, tmp);
  } 
  return ans;
}

__inline gsl_complex gsl_complex_poly_complex_eval(const gsl_complex c[], const int len, const gsl_complex z)
{
  int i;
  gsl_complex ans = c[len-1];
  for(i=len-1; i>0; i--) {
    /* The following three lines are equivalent to
       ans = gsl_complex_add (c[i-1], gsl_complex_mul (x, ans));
       but faster */
    double tmp = GSL_REAL (c[i-1]) + GSL_REAL (z) * GSL_REAL (ans) - GSL_IMAG (z) * GSL_IMAG (ans);
    GSL_SET_IMAG (&ans, GSL_IMAG (c[i-1]) + GSL_IMAG (z) * GSL_REAL (ans) + GSL_REAL (z) * GSL_IMAG (ans));
    GSL_SET_REAL (&ans, tmp);
  }
  return ans;
}

/* Solve for the complex roots of a general real polynomial */
typedef struct 
{ 
  size_t nc ;
  double * matrix ; 
} gsl_poly_complex_workspace ;

gsl_poly_complex_workspace * gsl_poly_complex_workspace_alloc (size_t n);
void gsl_poly_complex_workspace_free (gsl_poly_complex_workspace * w);
int gsl_poly_complex_solve (const double * a, size_t n, gsl_poly_complex_workspace * w, gsl_complex_packed_ptr z);

// external

#if defined(__CYGWIN32__)
#define C_EXPORT __declspec(dllexport)
#elif defined(WIN32) || defined(_WIN32) || defined(__WIN32__) || defined(_WIN64) || defined(WINAPI_FAMILY)
#define C_EXPORT __declspec(dllexport)
#elif defined(__MACH__) || defined(__ANDROID__) || defined(__linux__) || defined(__QNX__)
#define C_EXPORT
#else
#define C_EXPORT
#endif

C_EXPORT void poly_roots(int size, double* c, double* z);

__END_DECLS

#endif /* __GSL_POLY_H__ */
