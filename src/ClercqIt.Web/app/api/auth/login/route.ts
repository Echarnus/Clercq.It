import { NextRequest, NextResponse } from "next/server";

// Scaleway IAM validation endpoint
// In a production environment, this would verify credentials against Scaleway IAM
// For now, we'll use the existing JWT authentication system
export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    const { accessKey, secretKey } = body;

    // Validate input
    if (!accessKey || !secretKey) {
      return NextResponse.json(
        { message: "Access key and secret key are required" },
        { status: 400 }
      );
    }

    // For Scaleway IAM integration, we'll validate the credentials
    // and generate a JWT token using the backend API
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";
    
    // Call the backend authentication endpoint
    const response = await fetch(`${apiUrl}/api/auth/token`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        accessKey,
        secretKey,
      }),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({
        message: "Invalid credentials",
      }));
      return NextResponse.json(
        { message: errorData.message || "Authentication failed" },
        { status: response.status }
      );
    }

    const data = await response.json();
    
    return NextResponse.json({
      token: data.token,
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
