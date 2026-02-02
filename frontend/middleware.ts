import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

export function middleware(request: NextRequest) {
  const token = request.cookies.get('sazkomat_token')?.value;
  const { pathname } = request.nextUrl;

  // Allow access to: /, /api/*, /_next/*, static files, favicon, /recipes
  if (
    pathname === '/' ||
    pathname.startsWith('/api/') ||
    pathname.startsWith('/_next/') ||
    pathname.includes('.') ||
    pathname === '/favicon.ico' ||
    pathname === '/recipes'  // Temporarily allow recipes without auth
  ) {
    return NextResponse.next();
  }

  // Without token, redirect to landing page
  if (!token) {
    return NextResponse.redirect(new URL('/', request.url));
  }

  return NextResponse.next();
}

export const config = {
  matcher: [
    /*
     * Match all request paths except:
     * - _next/static (static files)
     * - _next/image (image optimization files)
     * - favicon.ico (favicon file)
     */
    '/((?!_next/static|_next/image|favicon.ico).*)',
  ],
};
