import { z } from 'zod';
import { getApiUrl } from '@/lib/api/baseApi';

// Certification schema
export const CertificationSchema = z.object({
  id: z.string().uuid(),
  title: z.string(),
  issuer: z.string(),
  issueDate: z.string(),
  expiryDate: z.string().nullable(),
  credentialId: z.string(),
  credentialUrl: z.string(),
  description: z.string(),
  image: z.string(),
});

export type Certification = z.infer<typeof CertificationSchema>;

export async function fetchAllCertifications(): Promise<Certification[]> {
  try {
    const response = await fetch(`${getApiUrl()}/api/certifications`, {
      cache: 'no-store',
    });

    if (!response.ok) {
      console.error('Failed to fetch certifications:', response.statusText);
      return [];
    }

    const data = await response.json();
    const validationResult = z.array(CertificationSchema).safeParse(data);

    if (!validationResult.success) {
      console.error('Invalid certification data:', validationResult.error);
      return [];
    }

    // Sort by issue date, newest first
    return validationResult.data.sort((a, b) =>
      new Date(b.issueDate).getTime() - new Date(a.issueDate).getTime()
    );
  } catch (error) {
    console.error('Error fetching certifications:', error);
    return [];
  }
}

export async function fetchCertificationById(id: string): Promise<Certification | null> {
  try {
    const response = await fetch(`${getApiUrl()}/api/certifications/${id}`, {
      cache: 'no-store',
    });

    if (!response.ok) {
      return null;
    }

    const data = await response.json();
    const validationResult = CertificationSchema.safeParse(data);

    if (!validationResult.success) {
      console.error('Invalid certification data:', validationResult.error);
      return null;
    }

    return validationResult.data;
  } catch (error) {
    console.error('Error fetching certification:', error);
    return null;
  }
}
