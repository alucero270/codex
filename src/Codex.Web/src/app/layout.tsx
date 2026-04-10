import type { Metadata } from "next";
import "../styles.css";

export const metadata: Metadata = {
  title: "Strata Search",
  description: "Search indexed knowledge artifacts and open source-bound previews."
};

export default function RootLayout({
  children
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
