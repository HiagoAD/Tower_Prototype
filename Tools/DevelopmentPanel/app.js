"use strict";

const $ = (query) => document.querySelector(query);
const escapeHtml = (value) => String(value ?? "").replace(/[&<>"']/g, c => ({"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;","'":"&#39;"}[c]));
const clean = value => String(value ?? "").replace(/[*`]/g, "");
const e = value => escapeHtml(clean(value));
const fileUrl = path => "/files/" + path.split("/").map(encodeURIComponent).join("/");
const badge = state => `<span class="badge ${state.toLowerCase().replaceAll(" ", "-")}">${e(state)}</span>`;
const time = iso => iso ? new Date(iso).toLocaleTimeString("en-GB", {timeZone:"America/Recife",hour:"2-digit",minute:"2-digit"}) : "—";
const date = iso => iso ? new Date(iso).toLocaleDateString("en-GB", {timeZone:"America/Recife",day:"2-digit",month:"short"}) : "Not recorded";
const stamp = iso => iso ? `${date(iso)}, ${time(iso)} Recife` : "Not recorded";
const size = bytes => bytes >= 1048576 ? `${(bytes / 1048576).toFixed(1)} MB` : `${Math.max(1, Math.round(bytes / 1024))} KB`;
const docButton = (path, label, classes="button") => `<button class="${classes}" data-doc="${escapeHtml(path)}">${escapeHtml(label)} <span aria-hidden="true">↗</span></button>`;
const empty = label => `<div class="empty">${escapeHtml(label)}</div>`;
const pages = {
  overview:["Development overview", "From the first Android build to a verified submission.", "Overview"],
  milestones:["Delivery milestones", "Recorded gate results and the evidence needed to move forward.", "Milestones"],
  evidence:["Evidence & builds", "Inspect the artifacts behind the development reports.", "Evidence & builds"],
  risks:["Risks & decisions", "Open considerations, ownership, and the next action on record.", "Risks & decisions"],
  documents:["Project documents", "The brief, working plan, handoff, and review history in one place.", "Project documents"]
};
const descriptions = {
  "CURRENT_FIDELITY_REVIEW.md":["Current fidelity checkpoint", "Image-directed correction, reviewed work, evidence limits, and the immediate execution order."],
  "ACTION_PLAN.md":["Action plan", "Delivery schedule, acceptance criteria, technical decisions, and recovery rules."],
  "CLAUDE_HANDOFF.md":["Implementation handoff", "Claude’s ownership, completion contract, and review checkpoints."],
  "STATUS.md":["Development status", "The source for gate results, evidence reports, risks, and quota observations."],
  "Unity-technical-test.md":["Original technical brief", "Immutable project requirements, webhook contract, and evaluation criteria."],
  "REFERENCE_BEHAVIOR_REVIEW.md":["Reference behavior review", "Observed mechanics, provisional assumptions, and unresolved questions."],
  "REFERENCE_OBSERVATION_ADDENDUM.md":["Reference observations", "Additional video observations and the proposed minimal gameplay interpretation."],
  "ASSET_REVIEW.md":["Asset & license review", "Free asset sources, license requirements, and sourcing decisions."],
  "README.md":["Project README", "Build, setup, controls, and delivery documentation."]
};
let data = null;
let fingerprint = "";
let busy = false;
let activePage = "overview";
let gateFilter = "all";
let gateSearch = "";
let documentSearch = "";
let modalRequest = 0;

function route() {
  const requested = location.hash.slice(1);
  activePage = pages[requested] ? requested : "overview";
  const [title, description, breadcrumb] = pages[activePage];
  $("#page-title").textContent = title;
  $("#page-description").textContent = description;
  $("#breadcrumb-page").textContent = breadcrumb;
  document.querySelectorAll(".page").forEach(page => page.hidden = page.id !== `page-${activePage}`);
  document.querySelectorAll("nav a").forEach(link => {
    const active = link.dataset.page === activePage;
    link.classList.toggle("active", active);
    if (active) link.setAttribute("aria-current", "page");
    else link.removeAttribute("aria-current");
  });
}

function countdown(iso) {
  if (!iso) return "Unscheduled";
  let delta = new Date(iso).getTime() - Date.now();
  const late = delta < 0;
  delta = Math.abs(delta);
  const hours = Math.floor(delta / 3600000);
  const minutes = Math.floor(delta % 3600000 / 60000);
  return `${hours}h ${String(minutes).padStart(2, "0")}m${late ? " past" : " left"}`;
}

function metric(label, value, note, symbol) {
  return `<article class="metric"><div class="metric-label">${label}<span class="metric-symbol" aria-hidden="true">${symbol}</span></div><div class="metric-value">${value}</div><div class="metric-note">${note}</div></article>`;
}

function gateRow(gate, next) {
  return `<button class="gate-row ${gate.status === "PASS" ? "pass" : ""} ${gate.id === next?.id ? "next" : ""}" data-gate="${e(gate.id)}">
    <span class="gate-marker">${gate.status === "PASS" ? "✓" : gate.id.slice(1)}</span><span class="gate-text"><span class="gate-title"><b>${e(gate.id)}</b> ${e(gate.name)}</span><span class="gate-time">${e(gate.time)} Recife${gate.id === next?.id ? " · Next unpassed gate" : ""}</span></span>${badge(gate.status)}<span class="gate-chevron" aria-hidden="true">›</span></button>`;
}

function renderOverview() {
  const d = data;
  const passed = d.gates.filter(g => g.status === "PASS").length;
  const next = d.gates.find(g => g.status !== "PASS");
  const percent = d.gates.length ? Math.round(passed / d.gates.length * 100) : 0;
  const latest = d.reports.at(-1);
  const statusNote = "Gate results come from STATUS.md. Artifact presence alone does not verify gameplay or pass a milestone.";
  const nextDue = next?.due && new Date(next.due) < new Date() ? "Target passed · review status" : "NEXT CHECKPOINT";
  $("#page-overview").innerHTML = `
    ${d.issues.map(issue => `<div class="alert">${e(issue)}</div>`).join("")}
    ${d.priority || d.roadmap?.length ? `<article class="card current-plan"><div class="card-header"><div><div class="eyebrow">IMMEDIATE PRIORITY</div><h2>Current execution plan</h2><p>${e(d.priority)}</p></div>${docButton("Docs/Development/ACTION_PLAN.md", "Plan", "small-link")}</div><div class="card-body">${(d.roadmap || []).map(step => `<div class="level-row"><div><h3>${e(step.order)} · ${e(step.work)}</h3><p>${e(step.evidence)}</p></div></div>`).join("")}${d.documents.some(doc => doc.name === "CURRENT_FIDELITY_REVIEW.md") ? docButton("Docs/Development/CURRENT_FIDELITY_REVIEW.md", "Open fidelity review") : ""}</div></article>` : ""}
    <div class="metrics">
      ${metric("Gates reported passed", `${passed} <small>/ ${d.gates.length}</small>`, `${percent}% of gates · not a feature-completion score`, "◷")}
      ${metric("Next checkpoint", next ? e(next.id) : "Complete", next ? e(next.name) : "All recorded gates passed", "⚑")}
      ${metric("Submission target", time(d.target), `${date(d.target)} · Recife <span class="mini-tag" id="countdown">${countdown(d.target)}</span>`, "◴")}
      ${metric("Level configurations", `${d.levelFiles.length} <small>/ 5 required</small>`, "Files found · playability not verified", "▥")}
    </div>
    <div class="overview-grid"><div class="column">
      <article class="card"><div class="card-header"><div><h2>Road to delivery</h2><p>Eight gates. One playable Android release.</p></div><a href="#milestones" class="small-link">View all ↗</a></div><div class="card-body"><div class="progress-summary"><span>Recorded milestones</span><strong>${passed} of ${d.gates.length} passed</strong></div><progress value="${passed}" max="${d.gates.length || 1}" aria-label="Milestones reported passed"></progress></div><div class="gate-list">${d.gates.map(g => gateRow(g,next)).join("") || empty("No milestone records available")}</div><div class="strip"><span class="dot"></span><span>Source: <strong>STATUS.md</strong> · ${stamp(d.statusUpdated)}</span>${docButton("Docs/Development/STATUS.md", "Read", "small-link")}</div></article>
      <article class="card"><div class="card-header"><div><h2>Latest development report</h2><p>Implementation evidence and the next handoff.</p></div>${latest ? '<button class="small-link" data-report="latest">Full report ↗</button>' : ""}</div><div class="card-body">${latest ? `<div class="report-date"><span class="report-indicator"></span>${e(latest.title)}</div><div class="report-preview">${e(latest.body.split("\n").filter(line => /Gate and result|Next action|Known gaps/.test(line)).join("\n") || latest.body.split("\n\n")[0])}</div>` : empty("No dated reports in STATUS.md")}</div><div class="strip"><span>Implementation</span><strong>Claude / quota fallback</strong><span>·</span><span>Milestone review</span><strong>Codex</strong></div></article>
      <article class="card"><div class="card-header"><div><h2>Five-level scope</h2><p>Planned progression from the action plan.</p></div><span class="badge">PLAN</span></div><div class="card-body">${d.levels.map((level,i) => `<div class="level-row"><span class="level-number">0${i+1}</span><div><h3>${e(level.name)}</h3><p>${e(level.plan)}</p></div></div>`).join("") || empty("No level plan found")}</div></article>
    </div><div class="column">
      <article class="card focus-card"><div class="card-body"><div class="focus-top"><span class="eyebrow">${nextDue}</span><span class="badge">${next ? e(next.id) : "DONE"}</span></div><h2>${next ? e(next.name.charAt(0).toUpperCase() + next.name.slice(1)) : "All gates reported passed"}</h2><p>${next ? e(next.acceptance) : "Check the final handoff and submission records."}</p><div class="focus-bottom"><span>${next ? `Target ${e(next.time)} · Recife` : "Review delivery evidence"}</span>${next ? `<button data-gate="${e(next.id)}" aria-label="Open ${e(next.id)} checkpoint details">View checkpoint ↗</button>` : docButton("Docs/Development/STATUS.md", "View report")}</div></div></article>
      <article class="card"><div class="card-header"><h2>Reference & brief</h2><span class="badge">SOURCE OF TRUTH</span></div><div class="card-body"><div class="reference-layout"><button class="evidence-item" data-image="Docs/Reference/Unity-technical-test/attachments/ref.png" aria-label="Open original reference image"><img class="reference-image" src="/files/Docs/Reference/Unity-technical-test/attachments/ref.png" alt="Original God Tower reference: a character climbing a pale tower against a blue sky"></button><div class="reference-copy"><h3>God Tower</h3><p>5 playable levels<br>Android APK + recording<br>Full-screen webhook event</p><div class="reference-actions">${docButton("Docs/Reference/Unity-technical-test/Unity-technical-test.md", "Brief", "small-link")}<button class="small-link" data-video="Docs/Reference/Unity-technical-test/attachments/ref.mp4">Video ↗</button></div></div></div><div class="integrity-line ${d.integrity.ok ? "" : "bad"}">${d.integrity.ok ? `✓ Reference preserved · ${d.integrity.files} checksums match` : `⚠ Reference check requires attention: ${e(d.integrity.issues.join(", "))}`}</div></div></article>
      <article class="card"><div class="card-header"><div><h2>What matters most</h2><p>Evaluation weights from the brief.</p></div></div><div class="card-body">${d.weights.map(w => `<div class="weight-row"><span class="weight-label">${e(w.category)}</span><span class="weight-track" aria-hidden="true">${Array.from({length:6},(_,i) => `<span class="weight-block ${i < parseInt(w.weight)/5 ? "filled" : ""}"></span>`).join("")}</span><strong class="weight-value">${e(w.weight)}</strong></div>`).join("")}</div></article>
      <article class="card"><div class="card-header"><h2>Workspace snapshot</h2><span class="badge">LOCAL</span></div><div class="card-body"><div class="artifact-row"><div><h3>Unity ${e(d.engine)}</h3><p>Editor version on disk</p></div><span class="badge">ANDROID</span></div><div class="artifact-row"><div><h3>${e(d.git.branch)} · ${e(d.git.revision)}</h3><p>${d.git.changed ?? "Unknown"} modified tracked files</p></div></div><div class="artifact-row"><div><h3>${d.apks.length} APK${d.apks.length === 1 ? "" : "s"} available</h3><p>${d.screenshots.length} screenshots · ${d.recordings.length} recordings</p></div><a href="#evidence" class="small-link">Inspect ↗</a></div></div></article>
    </div></div>
    <p class="source-note">${statusNote} Hard deadline: ${stamp(d.deadline)}. ${e(d.deadlineNote)}</p>`;
}

function renderGates() {
  $("#page-milestones").innerHTML = `<p class="section-intro">Open a milestone to see its recorded evidence and acceptance criteria. A missed target is a schedule signal; it does not change the recorded result.</p><div class="toolbar"><input class="search" id="gate-search" type="search" placeholder="Search milestones or evidence…" aria-label="Search milestones" value="${escapeHtml(gateSearch)}"><select id="gate-filter" aria-label="Filter milestone status"><option value="all">All statuses</option><option value="open">Unpassed gates</option><option value="PASS">Passed</option><option value="FAIL">Failed / blocked</option></select></div><div id="gate-results"></div>`;
  $("#gate-filter").value = gateFilter;
  renderGateResults();
}

function renderGateResults() {
  const gates = data.gates.filter(g => {
    const match = gateFilter === "all" || (gateFilter === "open" && g.status !== "PASS") || (gateFilter === "FAIL" && ["FAIL","BLOCKED"].includes(g.status)) || g.status === gateFilter;
    return match && `${g.id} ${g.name} ${g.evidence} ${g.acceptance}`.toLowerCase().includes(gateSearch.toLowerCase());
  });
  $("#gate-results").innerHTML = gates.map(g => `<article class="card milestone-card"><span class="gate-marker">${g.status === "PASS" ? "✓" : e(g.id)}</span><div><h2>${e(g.id)} — ${e(g.name)}</h2><p>${e(g.acceptance)}</p><button class="button" data-gate="${e(g.id)}">Inspect evidence <span aria-hidden="true">↗</span></button></div><div class="milestone-meta">${badge(g.status)}<p>Target ${e(g.time)} Recife</p>${g.status !== "PASS" && g.due && new Date(g.due) < new Date() ? '<span class="badge next">TARGET PASSED</span>' : ""}</div></article>`).join("") || empty("No milestones match this filter.");
}

function artifactRows(items, action="download") {
  return items.map(item => `<div class="artifact-row"><div><h3>${e(item.name)}</h3><p>${size(item.bytes)} · ${stamp(item.modified)}</p></div>${action === "document" ? docButton(item.path,"Open") : action === "video" ? `<button class="button" data-video="${escapeHtml(item.path)}">Watch ↗</button>` : `<a class="button" href="${fileUrl(item.path)}" download>Download ↓</a>`}</div>`).join("") || empty("No artifacts found in the tracked folders.");
}

function renderEvidence() {
  $("#page-evidence").innerHTML = `<div class="alert">These are files available on disk. Screenshots, APKs, and level configurations do not automatically pass a milestone. Consult the gate report for device and gameplay verification.</div><div class="artifact-grid"><article class="card"><div class="card-header"><div><h2>Android builds</h2><p>Builds/**/*.apk</p></div><span class="badge">${data.apks.length} FOUND</span></div><div class="card-body">${artifactRows(data.apks)}</div></article><article class="card"><div class="card-header"><div><h2>Device recordings</h2><p>Builds and development evidence</p></div><span class="badge">${data.recordings.length} FOUND</span></div><div class="card-body">${artifactRows(data.recordings,"video")}</div></article></div><div class="view-title"><h2>Screenshot evidence</h2><p>${data.screenshots.length} files · newest first</p></div><div class="evidence-grid">${data.screenshots.map(item => `<button class="card evidence-item" data-image="${escapeHtml(item.path)}"><div class="evidence-img-wrap"><img loading="lazy" src="${fileUrl(item.path)}?v=${encodeURIComponent(item.modified)}" alt="Development evidence: ${e(item.name)}"></div><div class="evidence-caption"><h3>${e(item.name)}</h3><p>${stamp(item.modified)} · ${size(item.bytes)}</p></div></button>`).join("") || empty("No screenshot evidence recorded yet.")}</div><div class="artifact-grid"><article class="card"><div class="card-header"><h2>Recent build logs</h2></div><div class="card-body">${artifactRows(data.logs,"document")}</div></article><article class="card"><div class="card-header"><div><h2>Imported license records</h2><p>Available records; not a complete license audit.</p></div></div><div class="card-body">${artifactRows(data.licenses,"document")}</div></article></div>`;
}

function renderRisks() {
  $("#page-risks").innerHTML = `<p class="section-intro">Items below are reproduced from “Known risks / decisions” in STATUS.md. Some may be resolved in a later report; update the source table when decisions change.</p><div class="risk-list">${data.risks.map((risk,i) => `<article class="card risk-card"><span class="badge">DECISION ${String(i+1).padStart(2,"0")}</span><h3>${e(risk.item)}</h3><p>${e(risk.action)}</p><div class="risk-owner">OWNER <strong>${e(risk.owner)}</strong></div></article>`).join("") || empty("No risk records found.")}</div><article class="card"><div class="card-header"><div><h2>Quota observations</h2><p>Reported observations only. Account usage is not connected.</p></div>${docButton("Docs/Development/STATUS.md", "Source", "small-link")}</div><div class="table-wrap"><table><thead><tr><th>Account / checkpoint</th><th>Session / reset</th><th>Weekly / reset</th><th>Notes</th></tr></thead><tbody>${data.quotas.map(q => `<tr><td>${e(q.account)}</td><td>${e(q.session)}</td><td>${e(q.weekly)}</td><td>${e(q.notes)}</td></tr>`).join("")}</tbody></table></div></article>`;
}

function renderDocuments() {
  $("#page-documents").innerHTML = `<div class="toolbar"><input class="search" id="document-search" type="search" aria-label="Search documents" placeholder="Find a project document…" value="${escapeHtml(documentSearch)}"></div><div class="docs-grid" id="document-results"></div>`;
  renderDocumentResults();
}

function renderDocumentResults() {
  $("#document-results").innerHTML = data.documents.filter(doc => `${doc.name} ${descriptions[doc.name]?.join(" ") || ""}`.toLowerCase().includes(documentSearch.toLowerCase())).map(doc => {
    const [title, description] = descriptions[doc.name] || [doc.name,"Project documentation."];
    return `<article class="card document-card"><span class="doc-icon" aria-hidden="true">▤</span><h2>${e(title)}</h2><p>${e(description)}</p><div class="doc-meta">${e(doc.path)}<br>Updated ${stamp(doc.modified)}</div>${docButton(doc.path,"Read document")}</article>`;
  }).join("") || empty("No documents match your search.");
}

function showDialog(title, content, eyebrow="PROJECT RECORD") {
  modalRequest++;
  $("#dialog-title").textContent = title;
  $("#dialog-eyebrow").textContent = eyebrow;
  $("#dialog-content").innerHTML = content;
  if (!$("#detail-dialog").open) $("#detail-dialog").showModal();
  return modalRequest;
}

async function showDocument(path) {
  const request = showDialog(descriptions[path.split("/").at(-1)]?.[0] || path.split("/").at(-1), '<p>Loading document…</p>', "READ-ONLY SOURCE DOCUMENT");
  try {
    const response = await fetch("/api/document?path=" + encodeURIComponent(path));
    if (!response.ok) throw new Error("This document is unavailable.");
    const doc = await response.json();
    if (request !== modalRequest) return;
    $("#dialog-content").innerHTML = `<div class="detail-actions"><a class="button" href="${fileUrl(path)}" download>Download source ↓</a></div>${doc.truncated ? '<div class="alert">Showing the last 250 KB of this log. Download it for the complete file.</div>' : ""}<pre class="document-content">${escapeHtml(doc.content)}</pre>`;
  } catch (error) {
    if (request === modalRequest) $("#dialog-content").textContent = error.message;
  }
}

function showGate(id) {
  const g = data.gates.find(g => g.id === id);
  if (!g) return;
  const shots = data.screenshots.filter(s => s.name.toLowerCase().startsWith(id.toLowerCase() + "_"));
  showDialog(`${g.id} — ${g.name}`, `${badge(g.status)}<div class="detail-section"><h3>Target</h3><p>${e(g.time)} Recife · ${date(g.due)}</p></div><div class="detail-section"><h3>Required evidence</h3><p>${e(g.acceptance)}</p></div><div class="detail-section"><h3>Recorded result</h3><p>${e(g.reportedStatus)}
${e(g.evidence)}</p></div>${shots.length ? `<div class="detail-section"><h3>Related screenshots</h3><p>Matched by filename; review the report before accepting this gate.</p>${shots.map(s => `<div class="artifact-row"><span>${e(s.name)}</span><button class="button" data-image="${escapeHtml(s.path)}">View ↗</button></div>`).join("")}</div>` : ""}<div class="detail-actions">${docButton("Docs/Development/STATUS.md","Read full status")}${docButton("Docs/Development/ACTION_PLAN.md","Action plan")}</div>`, "MILESTONE DETAIL");
}

async function refresh() {
  if (busy) return;
  busy = true;
  $("#refresh").disabled = true;
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 12000);
  try {
    const response = await fetch("/api/status", {signal:controller.signal});
    if (!response.ok) throw new Error(`Server returned ${response.status}`);
    const next = await response.json();
    const {fetchedAt, ...stable} = next;
    const signature = JSON.stringify(stable);
    data = next;
    if (signature !== fingerprint) {
      // Keep the focused filter input and its selection through automatic updates.
      const focused = document.activeElement;
      const inputState = focused?.tagName === "INPUT" ? {id:focused.id,start:focused.selectionStart,end:focused.selectionEnd} : null;
      renderOverview(); renderGates(); renderEvidence(); renderRisks(); renderDocuments();
      $("#nav-gate-count").textContent = data.gates.length;
      fingerprint = signature;
      if (inputState) {
        const input = document.getElementById(inputState.id);
        if (input) { input.focus({preventScroll:true}); input.setSelectionRange(inputState.start,inputState.end); }
      }
    }
    $("#loading").hidden = true;
    $("#content").hidden = false;
    $("#error").hidden = true;
    $("#connection-dot").classList.remove("offline");
    $("#sync-label").textContent = `Synced ${time(fetchedAt)}`;
    $("#sync-label").title = `Last successful refresh: ${stamp(fetchedAt)}`;
    route();
  } catch (error) {
    $("#loading").hidden = true;
    $("#error").hidden = false;
    $("#error").textContent = `Unable to refresh project records. ${data ? "Showing the last successful snapshot. " : ""}Check that the local panel server is running, then refresh. (${error.message})`;
    $("#sync-label").textContent = "Disconnected";
    $("#connection-dot").classList.add("offline");
  } finally {
    clearTimeout(timeout);
    busy = false;
    $("#refresh").disabled = false;
  }
}

document.addEventListener("click", event => {
  const button = event.target.closest("[data-doc],[data-gate],[data-image],[data-video],[data-report]");
  if (!button) return;
  if (button.dataset.doc) showDocument(button.dataset.doc);
  else if (button.dataset.gate && data) showGate(button.dataset.gate);
  else if (button.dataset.image) showDialog(button.dataset.image.split("/").at(-1), `<img class="detail-image" src="${fileUrl(button.dataset.image)}" alt="${e(button.dataset.image.split("/").at(-1))}">`, "IMAGE EVIDENCE");
  else if (button.dataset.video) showDialog(button.dataset.video.split("/").at(-1), `<video class="detail-video" controls preload="metadata" src="${fileUrl(button.dataset.video)}"></video>`, "VIDEO");
  else if (button.dataset.report && data?.reports.length) {
    const report = data.reports.at(-1);
    showDialog(report.title, `<pre class="document-content">${escapeHtml(report.body)}</pre>`, "IMPLEMENTATION REPORT");
  }
});
document.addEventListener("input", event => {
  if (event.target.id === "gate-search") { gateSearch = event.target.value; renderGateResults(); }
  if (event.target.id === "document-search") { documentSearch = event.target.value; renderDocumentResults(); }
});
document.addEventListener("change", event => {
  if (event.target.id === "gate-filter") { gateFilter = event.target.value; renderGateResults(); }
});
$("#refresh").addEventListener("click", refresh);
$("#close-dialog").addEventListener("click", () => $("#detail-dialog").close());
$("#detail-dialog").addEventListener("close", () => { modalRequest++; $("#dialog-content").innerHTML = ""; });
$("#detail-dialog").addEventListener("click", event => { if (event.target === $("#detail-dialog")) $("#detail-dialog").close(); });
window.addEventListener("hashchange", route);
document.addEventListener("visibilitychange", () => { if (!document.hidden) refresh(); });
setInterval(() => { if (!document.hidden) refresh(); }, 15000);
setInterval(() => { if (data && $("#countdown")) $("#countdown").textContent = countdown(data.target); }, 30000);
route();
refresh();
