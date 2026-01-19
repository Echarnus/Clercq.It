import { NextRequest, NextResponse } from "next/server";

// Cloud IAM authentication endpoint
export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    const { username, password, totpCode } = body;

    // Validate input
    if (!username || !password) {
      return NextResponse.json(
        { message: "Username and password are required" },
        { status: 400 }
      );
    }

    // Call the backend authentication endpoint
    // Default port 5000 matches the Aspire AppHost configuration
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";
    
    const response = await fetch(`${apiUrl}/api/auth/login`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        username,
        password,
        totpCode,
      }),
    });

    const data = await response.json().catch(() => ({
      message: "Invalid response from server",
    }));

    // Check if MFA is required (status 428)
    if (response.status === 428) {
      return NextResponse.json(
        { 
          requiresMfa: data.requiresMfa || true, 
          message: data.message || "MFA required" 
        },
        { status: 428 }
      );
    }

    if (!response.ok) {
      return NextResponse.json(
        { message: data.message || "Authentication failed" },
        { status: response.status }
      );
    }
    
    return NextResponse.json({
      token: data.token,
      user: data.user,
      expiresAt: data.expiresAt,
    });
  } catch (error) {
    console.error("Login error:", error);
    return NextResponse.json(
      { message: "An error occurred during authentication" },
      { status: 500 }
    );
  }
}
