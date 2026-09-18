import { useState } from "react";
import type { CSSProperties } from "react";
import { useNavigate } from "react-router-dom";
import {
  Avatar,
  Button,
  Card,
  Input,
  LoadingState,
  PageHeader,
  getApiBankService,
  imageUrls,
  useAuth,
  useBank,
} from "@minibank/shared/src/index";
import { getFullNameError, getPhoneError, normalizePhone } from "@minibank/shared/src/utils/authValidation";
import "./profile.css";

/** Feature: profile — view + edit name/phone + sign out (live API). */
export function ProfileScreen(): React.JSX.Element {
  const { profile, loading, error, refresh } = useBank();
  const { logout } = useAuth();
  const navigate = useNavigate();
  const [editing, setEditing] = useState(false);
  const [fullName, setFullName] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  if (loading) return <LoadingState message="Loading profile…" />;
  if (error || !profile) {
    return (
      <div className="mobile-screen">
        <Card>
          <p role="alert">{error ?? "Could not load profile."}</p>
        </Card>
      </div>
    );
  }

  const startEdit = (): void => {
    setFullName(profile.fullName);
    setPhoneNumber(profile.phoneNumber);
    setFieldErrors({});
    setSubmitError(null);
    setEditing(true);
  };

  const handleSave = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault();
    const errors: Record<string, string> = {};
    const nameError = getFullNameError(fullName);
    if (nameError) errors.fullName = nameError;
    const phoneError = getPhoneError(phoneNumber);
    if (phoneError) errors.phoneNumber = phoneError;
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) return;
    setSaving(true);
    setSubmitError(null);
    try {
      await getApiBankService().updateProfile(fullName.trim(), normalizePhone(phoneNumber));
      await refresh();
      setEditing(false);
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : "Could not update your profile.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="mobile-screen">
      <PageHeader title="My Profile" subtitle="Personal banking info." />
      <Card>
        <div
          className="profile-hero ocean-placeholder"
          style={{ "--ocean-image": `url("${imageUrls.profileBackground}")` } as CSSProperties}
        >
          <Avatar name={profile.fullName} size={60} />
          <p className="profile-name">{profile.fullName}</p>
          <p className="profile-account">{profile.accountNumber}</p>
        </div>
        {!editing ? (
          <>
            <dl className="profile-rows">
              <div>
                <dt>Full Name</dt>
                <dd>{profile.fullName}</dd>
              </div>
              <div>
                <dt>Account Number</dt>
                <dd className="mono">{profile.accountNumber}</dd>
              </div>
              <div>
                <dt>Phone Number</dt>
                <dd>{profile.phoneNumber}</dd>
              </div>
              <div>
                <dt>Email</dt>
                <dd>{profile.email}</dd>
              </div>
            </dl>
            <div style={{ display: "flex", gap: 8, marginTop: 12 }}>
              <Button variant="secondary" onClick={startEdit}>
                Edit
              </Button>
              <Button
                variant="ghost"
                onClick={() => {
                  logout();
                  navigate("/login", { replace: true });
                }}
              >
                Sign out
              </Button>
            </div>
          </>
        ) : (
          <form onSubmit={(e) => void handleSave(e)} noValidate>
            <Input
              label="Full name"
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              error={fieldErrors.fullName}
            />
            <Input
              label="Phone number"
              value={phoneNumber}
              onChange={(e) => setPhoneNumber(e.target.value)}
              error={fieldErrors.phoneNumber}
            />
            {submitError ? <p role="alert">{submitError}</p> : null}
            <div style={{ display: "flex", gap: 8, marginTop: 12 }}>
              <Button type="submit" loading={saving}>
                Save
              </Button>
              <Button variant="ghost" onClick={() => setEditing(false)}>
                Cancel
              </Button>
            </div>
          </form>
        )}
      </Card>
    </div>
  );
}
