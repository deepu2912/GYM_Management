import { useEffect, useMemo, useState } from "react";
import { api } from "../api/client";
import { useAuth } from "../context/AuthContext";
import { Link } from "react-router-dom";

function ProfilePage() {
  const { user, getApiErrorMessage, setProfilePhoto } = useAuth();
  const [profilePhotoDataUri, setProfilePhotoDataUri] = useState<string | null>(user?.profilePhotoDataUri ?? null);
  const [loadingPhoto, setLoadingPhoto] = useState(true);
  const [uploadingPhoto, setUploadingPhoto] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const initials = useMemo(() => {
    const parts = user?.name?.trim()?.split(/\s+/) ?? [];
    return `${parts[0]?.[0] ?? ""}${parts[1]?.[0] ?? ""}`.toUpperCase() || "U";
  }, [user?.name]);

  useEffect(() => {
    let mounted = true;
    const loadProfile = async () => {
      setLoadingPhoto(true);
      try {
        const response = await api.get("/api/auth/profile");
        if (mounted) {
          const nextPhoto = response.data?.profilePhotoDataUri ?? null;
          setProfilePhotoDataUri(nextPhoto);
          setProfilePhoto(nextPhoto);
        }
      } catch {
        // Keep existing auth context value when this optional fetch fails.
      } finally {
        if (mounted) {
          setLoadingPhoto(false);
        }
      }
    };

    loadProfile();
    return () => {
      mounted = false;
    };
  }, []);

  const handlePhotoChange = (event) => {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    if (!file.type.startsWith("image/")) {
      setError("Please select an image file.");
      setSuccess("");
      return;
    }

    setUploadingPhoto(true);
    setError("");
    setSuccess("");

    const reader = new FileReader();
    reader.onload = async () => {
      try {
        const photoDataUri = typeof reader.result === "string" ? reader.result : "";
        if (!photoDataUri) {
          throw new Error("Unable to read selected image.");
        }

        const response = await api.put("/api/auth/profile-photo", { photoDataUri });
        const nextPhoto = response.data?.profilePhotoDataUri ?? photoDataUri;
        setProfilePhotoDataUri(nextPhoto);
        setProfilePhoto(nextPhoto);
        setSuccess("Profile photo updated successfully.");
      } catch (err) {
        setError(getApiErrorMessage(err, "Failed to update profile photo."));
      } finally {
        setUploadingPhoto(false);
      }
    };

    reader.onerror = () => {
      setUploadingPhoto(false);
      setError("Failed to process selected image.");
    };

    reader.readAsDataURL(file);
  };

  return (
    <section>
      <h2 className="text-2xl font-bold text-slate-900">Profile</h2>
      <p className="mt-1 text-sm text-slate-600">Account details and role information.</p>

      <div className="mt-5 rounded-2xl border border-amber-100 bg-white p-5 shadow-sm">
        <div className="mb-5 flex items-center gap-4 rounded-xl border border-slate-200 bg-slate-50 p-4">
          <div className="flex h-20 w-20 items-center justify-center overflow-hidden rounded-full border border-slate-300 bg-white">
            {profilePhotoDataUri ? (
              <img src={profilePhotoDataUri} alt="Profile photo" className="h-full w-full object-cover" />
            ) : (
              <span className="text-xl font-bold text-slate-500">{initials}</span>
            )}
          </div>
          <div>
            <p className="text-sm font-semibold text-slate-700">Profile Photo</p>
            <label className="mt-2 inline-flex cursor-pointer rounded-lg bg-orange-500 px-3 py-2 text-sm font-semibold text-white hover:bg-orange-600">
              {uploadingPhoto ? "Uploading..." : "Upload New Photo"}
              <input
                type="file"
                accept="image/*"
                onChange={handlePhotoChange}
                disabled={uploadingPhoto}
                className="hidden"
              />
            </label>
            {loadingPhoto && <p className="mt-2 text-xs text-slate-500">Loading current photo...</p>}
          </div>
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Name</p>
            <p className="mt-1 text-lg font-semibold text-slate-900">{user?.name ?? "-"}</p>
          </div>
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Email</p>
            <p className="mt-1 text-lg font-semibold text-slate-900">{user?.email ?? "-"}</p>
          </div>
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Role</p>
            <p className="mt-1 text-lg font-semibold text-slate-900">{user?.role ?? "-"}</p>
          </div>
        </div>
        {error && <p className="mt-4 text-sm font-medium text-red-600">{error}</p>}
        {success && <p className="mt-4 text-sm font-medium text-emerald-600">{success}</p>}
        <div className="mt-5">
          <Link
            to="/reset-password"
            className="inline-flex rounded-lg bg-orange-500 px-4 py-2 text-sm font-semibold text-white hover:bg-orange-600"
          >
            Reset Password
          </Link>
        </div>
      </div>
    </section>
  );
}

export default ProfilePage;
