"use client"

import Link from "next/link";

export function FullFooter() {
    return (
        <div className="bg-base-200 text-base-content w-full min-h-37.5 pt-5 pb-16 mt-auto border-t border-neutral-400/25" suppressHydrationWarning>
            <div className="lg:container mx-auto px-2">
                <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3 text-sm">
                        © {new Date().getFullYear()} Jean-Michel Gaud.
                        <Link href="https://www.linkedin.com/in/jean-michel-gaud-124304b/" target="_blank" rel="noopener noreferrer" className="hover:text-primary">
                            LinkedIn
                        </Link>
                        <Link href="https://github.com/wirelessjeano" target="_blank" rel="noopener noreferrer" className="hover:text-primary">
                            GitHub
                        </Link>
                    </div>

                </div>
            </div>
        </div>
    );
}
